using System;
using System.Collections.Generic;
using System.IO;
namespace G870Windows {
    sealed class FakeTransport : ITransport {
        public uint Cpu=51,Gpu=43,Rpm=3000,Mode=2,Mux=1,External=0;
        public bool FailSensors,FailRestore;
        public List<byte[]> Writes=new List<byte[]>();
        public byte[] Exchange(byte[] request){
            var b=(byte[])request.Clone();ushort kind=BitConverter.ToUInt16(b,0),cmd=BitConverter.ToUInt16(b,2);
            if(kind==0xfb00){
                Writes.Add(b);if(cmd==0x300)Mode=BitConverter.ToUInt32(b,4);
                if(cmd==0x205 && BitConverter.ToUInt32(b,4)==255 && FailRestore)throw new IOException("restore unavailable");
            } else if(cmd==0x200){if(FailSensors)throw new IOException("sensor unavailable");Copy(b,4,Cpu);Copy(b,8,Gpu);Copy(b,12,Rpm);Copy(b,16,Rpm);}
            else if(cmd==0x203){Copy(b,4,Mux);Copy(b,8,External);Copy(b,12,Mux);}
            else if(cmd==0x300)Copy(b,4,Mode);
            return b;
        }
        static void Copy(byte[] b,int p,uint n){Buffer.BlockCopy(BitConverter.GetBytes(n),0,b,p,4);}
        public uint LastFan(){for(int i=Writes.Count-1;i>=0;i--)if(BitConverter.ToUInt16(Writes[i],2)==0x205)return BitConverter.ToUInt32(Writes[i],4);return 0;}
    }
    sealed class FakePowerPlanLink : IPowerPlanLink {public readonly List<int> Modes=new List<int>();public void ApplyMode(int mode){Modes.Add(mode);}}
    static class Tests {
        static int count;
        static void Assert(bool ok,string label){if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS "+label);}
        static void Reject(Action a,string label){bool rejected=false;try{a();}catch{rejected=true;}Assert(rejected,label);}
        public static int Main(){try{
            var p=new Profile{Fan="curve"};var f=new FakeTransport();var engine=new Engine(new Controller(f));
            engine.Apply(2,p);Assert(f.LastFan()>=35 && f.LastFan()<=100,"curve activates within fan limits");
            f.Cpu=80;engine.Tick();Assert(f.LastFan()==255 && engine.Read().Fan=="auto","80C restores BIOS");
            f.Cpu=51;int n=f.Writes.Count;engine.Tick();Assert(f.Writes.Count==n,"cooled system does not silently rearm curve");
            engine.Apply(2,p);f.FailSensors=true;Reject(()=>engine.Tick(),"sensor loss reports failure");f.FailSensors=false;Assert(f.LastFan()==255,"sensor loss restores BIOS");
            engine.Apply(2,p);f.Mode=1;engine.Tick();Assert(f.LastFan()==255,"external mode change restores BIOS");
            engine.Apply(2,p);f.Rpm=0;for(int i=0;i<5;i++)engine.Tick();Assert(f.LastFan()==255,"fan stall after grace restores BIOS");f.Rpm=3000;
            engine.Apply(2,p);f.FailRestore=true;Reject(()=>engine.Restore(),"restoration failure is retained");Assert(engine.Read().RestorePending,"failed restore blocks further control");f.FailRestore=false;engine.Tick();Assert(!engine.Read().RestorePending && f.LastFan()==255,"restoration retries successfully");
            var invalid=new Profile();invalid.Curve[3].Temperature=invalid.Curve[2].Temperature;Reject(()=>engine.Apply(2,invalid),"unordered curve rejected before hardware access");
            var c=new Controller(f);Reject(()=>c.SetFan(34),"fan below minimum rejected");Reject(()=>c.SetMode(3),"unknown mode rejected");
            var color=new Profile{RgbEnabled=true,Color=0x123456,Brightness=2,Effect="1"};c.SetRgb(color,51,0);var packet=f.Writes[f.Writes.Count-1];
            Assert(BitConverter.ToUInt16(packet,2)==0x100 && BitConverter.ToUInt32(packet,4)==6 && BitConverter.ToUInt32(packet,8)==0x12123456,"RGB packet matches firmware DAT2 and DAT3 layout");
            color.ZonesEnabled=true;color.Zones=new[]{0xff0000,0x00ff00,0x0000ff};c.SetRgb(color,51,0);
            Assert(BitConverter.ToUInt32(f.Writes[f.Writes.Count-3],4)==5 && BitConverter.ToUInt32(f.Writes[f.Writes.Count-1],4)==3,"left middle right zones map to 5 4 3");
            var data=new Settings();data.Profiles[2]=p;string path=Path.Combine(Path.GetTempPath(),"g870-test-"+Guid.NewGuid()+".json");
            try{ProfileStore.Write(path,data);Assert(ProfileStore.Read(path).Profiles[2].Fan=="curve","profile round trip");File.WriteAllText(path,"{\"Version\":99}");Reject(()=>ProfileStore.Read(path),"unsupported profile schema rejected");}finally{if(File.Exists(path))File.Delete(path);}
            Assert(CurvePolicy.Interpolate(p.Curve,57)==40,"curve interpolation rounds upward");
            Assert(c.ReadMux().Supported && c.ReadMux().Current==1,"hybrid firmware mode recognized");
            for(uint target=1;target<=3;target++){c.SetMux(target);var muxPacket=f.Writes[f.Writes.Count-1];Assert(BitConverter.ToUInt16(muxPacket,0)==0xfb00 && BitConverter.ToUInt16(muxPacket,2)==0x203 && BitConverter.ToUInt32(muxPacket,4)==target,"MUX request packet mode "+target);}
            f.External=1;Reject(()=>c.SetMux(3),"external display blocks UMA");f.External=0;
            Reject(()=>c.SetMux(0),"invalid MUX target rejected");
            f.Mux=99;Reject(()=>c.SetMux(1),"unknown firmware mode blocks writes");
            f.Mux=3;f.Gpu=0;var uma=c.Read();Assert(!uma.GpuAvailable && uma.Gpu==0,"UMA keeps CPU monitoring when NVIDIA sensor is absent");
            Assert(CurvePolicy.Decide(p,uma,255,5)!=255,"CPU-only curve ignores absent NVIDIA fan");
            f.Mux=1;Reject(()=>c.Read(),"GPU sensor failure in hybrid is not hidden");
            f.Gpu=43;f.Cpu=51;f.Rpm=3000;f.Mode=2;
            var rgbEngine=new Engine(new Controller(f));rgbEngine.Apply(2,new Profile{Fan="curve"});
            var oldFan=rgbEngine.Read();int beforeRgb=f.Writes.Count;
            var white=new Profile{RgbEnabled=true,Color=0xffffff};rgbEngine.ApplyRgb(2,white);
            for(int i=beforeRgb;i<f.Writes.Count;i++)Assert(BitConverter.ToUInt16(f.Writes[i],2)==0x100,"RGB-only apply sends no fan or mode writes");
            Assert(rgbEngine.Read().Fan==oldFan.Fan && rgbEngine.Read().Duty==oldFan.Duty,"RGB-only apply preserves active curve");
            Reject(()=>rgbEngine.ApplyRgb(1,white),"RGB-only rejects mismatched performance profile");
            var saved=new Settings();saved.LastRgb=white.RgbCopy();saved.Profiles[0].Color=0xff;white.Color=0x00ff00;
            Assert(saved.LastRgb.Color==0xffffff,"last applied RGB is copied independently of editable profiles");
            string rgbPath=Path.Combine(Path.GetTempPath(),"g870-rgb-test-"+Guid.NewGuid()+".json");
            try{ProfileStore.Write(rgbPath,saved);Assert(ProfileStore.Read(rgbPath).LastRgb.Color==0xffffff,"white RGB survives serialization and reload");}finally{if(File.Exists(rgbPath))File.Delete(rgbPath);}
            var original=new Profile{Fan="curve",RgbEnabled=true,Color=0xffffff};var copied=original.Copy();original.Curve[0].Duty=36;original.Zones[0]=0;
            Assert(copied.Curve[0].Duty==35 && copied.Zones[0]==255,"custom profile snapshot does not share mutable arrays");
            var named=new NamedProfile{Name="Gece",Mode=2,Value=copied};named.Validate();var namedSettings=new Settings();namedSettings.CustomProfiles.Add(named);
            string namedPath=Path.Combine(Path.GetTempPath(),"g870-custom-"+Guid.NewGuid()+".json");
            try{ProfileStore.Write(namedPath,namedSettings);var loaded=ProfileStore.Read(namedPath);Assert(loaded.CustomProfiles.Count==1 && loaded.CustomProfiles[0].Name=="Gece" && loaded.CustomProfiles[0].Value.Fan=="curve","named profiles survive disk round trip");}finally{if(File.Exists(namedPath))File.Delete(namedPath);}
            namedSettings.CustomProfiles.Add(new NamedProfile{Name="gece",Mode=1,Value=new Profile()});Reject(()=>namedSettings.Validate(),"duplicate custom names rejected case-insensitively");
            Reject(()=>new NamedProfile{Name=" ",Mode=2,Value=new Profile()}.Validate(),"empty custom name rejected");
            var legacy=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Settings>("{\"Version\":1,\"LastMode\":2}");legacy.Validate();Assert(legacy.CustomProfiles.Count==0,"legacy settings initialize empty custom list");
            Assert(WindowsPowerPlans.Identify(new Guid("5da85d85-eb0c-4710-a805-6da8bbcf1f02"),"Renamed")==2,"OEM Office GUID survives a display-name change");
            Assert(!WindowsPowerPlans.Identify(Guid.NewGuid(),"Gaming").HasValue,"matching display name alone is not an OEM plan identity");
            Assert(!WindowsPowerPlans.Identify(new Guid("381b4222-f694-41f0-9685-ff5bb260df2e"),"Dengeli").HasValue,"Balanced is not misidentified as Office");
            Assert(!WindowsPowerPlans.Identify(Guid.NewGuid(),"Nihai Performans").HasValue,"Ultimate is not misidentified as OEM High performance");
            var duplicatePlans=new List<PowerPlan>{new PowerPlan{Id=Guid.NewGuid(),Name="Gaming",Mode=1},new PowerPlan{Id=Guid.NewGuid(),Name="Gaming",Mode=1}};
            Reject(()=>WindowsPowerPlans.Resolve(1,duplicatePlans),"ambiguous power-plan names rejected");
            var powerLink=new FakePowerPlanLink();var powerEngine=new Engine(new Controller(new FakeTransport()),powerLink);powerEngine.Apply(2,new Profile());Assert(powerLink.Modes.Count==1 && powerLink.Modes[0]==2,"application mode propagates to power-plan link");
            powerEngine.ApplyRgb(2,new Profile{RgbEnabled=true,Color=0xffffff});Assert(powerLink.Modes.Count==1,"RGB does not change the Windows power plan");
            Console.WriteLine("TOTAL "+count+" checks passed");return 0;
        }catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}}
    }
}
