using System;
using System.Drawing;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace G870Windows {
    interface ITransport { byte[] Exchange(byte[] request); }
    sealed class WmiTransport : ITransport {
        public byte[] Exchange(byte[] request) {
            var scope = new ManagementScope(@"\\.\root\wmi");
            scope.Options.EnablePrivileges = true;
            scope.Options.Timeout = TimeSpan.FromSeconds(5);
            var options = new EnumerationOptions { Timeout = TimeSpan.FromSeconds(5) };
            using (var search = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM RW_GMWMI"), options))
            using (var items = search.Get()) {
                ManagementObject target = null;
                foreach (ManagementObject item in items) {
                    if (target != null) { item.Dispose(); target.Dispose(); throw new IOException("Birden fazla RW_GMWMI aygıtı bulundu."); }
                    target = item;
                }
                if (target == null) throw new IOException("RW_GMWMI bulunamadı. BIOS/ACPI WMI desteğini ve Windows WmiAcpi aygıtını kontrol edin.");
                using (target) {
                    target["BufferBytes"] = (byte[])request.Clone();
                    target.Put(new PutOptions { Timeout = TimeSpan.FromSeconds(5) });
                    target.Get();
                    var result = target["BufferBytes"] as byte[];
                    if (result == null || result.Length != 32) throw new IOException("Firmware cevabı 32 bayt değil.");
                    return (byte[])result.Clone();
                }
            }
        }
    }
    sealed class DemoTransport : ITransport {
        uint mode = 2;
        public byte[] Exchange(byte[] request) {
            byte[] b = (byte[])request.Clone();
            ushort kind = BitConverter.ToUInt16(b, 0), command = BitConverter.ToUInt16(b, 2);
            if (kind == 0xfb00 && command == 0x300) mode = BitConverter.ToUInt32(b, 4);
            if (kind == 0xfa00 && command == 0x203) { Copy(b,4,1);Copy(b,12,1); }
            if (kind == 0xfa00 && command == 0x300) Copy(b, 4, mode);
            if (kind == 0xfa00 && command == 0x200) {
                Copy(b, 4, 51); Copy(b, 8, 43); Copy(b, 12, 3406); Copy(b, 16, 3011);
            }
            return b;
        }
        static void Copy(byte[] b, int offset, uint value) { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, b, offset, 4); }
    }
    sealed class Snapshot {
        public uint Cpu, Gpu, CpuRpm, GpuRpm, Mode;
        public bool GpuAvailable=true;
    }
    sealed class Controller {
        readonly ITransport transport;
        readonly object sync = new object();
        public Controller(ITransport value) { transport = value; }
        public static byte[] Packet(ushort kind, ushort command, uint a, uint b) {
            byte[] data = new byte[32];
            Buffer.BlockCopy(BitConverter.GetBytes(kind), 0, data, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(command), 0, data, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(a), 0, data, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(b), 0, data, 8, 4);
            return data;
        }
        byte[] Call(ushort kind, ushort command, uint a, uint b) {
            byte[] reply = transport.Exchange(Packet(kind, command, a, b));
            if (reply == null || reply.Length != 32 || BitConverter.ToUInt16(reply, 0) == 0x1000)
                throw new IOException("Firmware komutu reddetti veya cevap geçersiz.");
            return reply;
        }
        public Snapshot Read() {
            lock (sync) {
                byte[] status = Call(0xfa00, 0x200, 0, 0), mode = Call(0xfa00, 0x300, 0, 0);
                var s = new Snapshot { Cpu = BitConverter.ToUInt32(status,4), Gpu = BitConverter.ToUInt32(status,8),
                    CpuRpm = BitConverter.ToUInt32(status,12), GpuRpm = BitConverter.ToUInt32(status,16), Mode = BitConverter.ToUInt32(mode,4) };
                if(s.Gpu<5 || s.Gpu>110){
                    var mux=ReadMux();
                    if(!mux.Supported || mux.Current!=3)throw new IOException("GPU sensör değeri geçersiz.");
                    s.GpuAvailable=false;s.Gpu=0;s.GpuRpm=0;
                }
                if (s.Cpu < 5 || s.Cpu > 110 || s.CpuRpm > 30000 || s.GpuRpm > 30000 || s.Mode > 2)
                    throw new IOException("Donanım değerleri beklenen aralığın dışında.");
                return s;
            }
        }
        public void SetMode(uint value) {
            if (value > 2) throw new ArgumentOutOfRangeException("value");
            lock (sync) {
                Call(0xfb00, 0x300, value, 0);
                if (BitConverter.ToUInt32(Call(0xfa00, 0x300, 0, 0), 4) != value) throw new IOException("Performans modu geri okunarak doğrulanamadı.");
            }
        }
        public MuxState ReadMux(){
            lock(sync){
                byte[] reply=Call(0xfa00,0x203,0,0);
                uint current=BitConverter.ToUInt32(reply,4),boot=BitConverter.ToUInt32(reply,12);
                bool supported=current>=1 && current<=3 && boot>=1 && boot<=3;
                return new MuxState{Current=current,Boot=boot,ExternalDisplay=BitConverter.ToUInt32(reply,8),Supported=supported,Message=supported?"Yeniden başlatma gerektirir.":"MUX durumu bu firmware'de doğrulanamadı."};
            }
        }
        public void SetMux(uint value){
            if(value<1 || value>3)throw new ArgumentOutOfRangeException("value");
            lock(sync){
                var state=ReadMux();
                if(!state.Supported)throw new IOException(state.Message);
                if(value==3 && state.ExternalDisplay!=0)throw new IOException("Dahili GPU moduna geçmeden önce harici ekran bağlantısını çıkarın.");
                Call(0xfb00,0x203,value,0);
            }
        }
        public void SetRgb(Profile profile, uint temperature, int step) {
            profile.Validate();
            if (!profile.RgbEnabled) return;
            for (int i=0;i<(profile.ZonesEnabled?3:1);i++) {
                int color = profile.ZonesEnabled ? profile.Zones[i] : Effects.ColorFor(profile, temperature, step);
                uint effect = profile.ZonesEnabled ? 1u : Effects.NativeMode(profile.Effect);
                uint packed = (effect << 28) | ((uint)profile.Brightness << 24) | ((uint)color & 0xffffff);
                // Firmware WSAA FB00/0100: DAT2=zone, DAT3=B/G/R/brightness/mode.
                lock(sync) Call(0xfb00, 0x100, profile.ZonesEnabled ? (uint)(5-i) : 6u, packed);
            }
        }
        public void SetFan(uint duty) {
            if ((duty < 35 || duty > 100) && duty != 255) throw new ArgumentOutOfRangeException("duty");
            lock (sync) Call(0xfb00, 0x205, duty, duty);
        }
    }
}
