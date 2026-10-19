using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace G870Windows {
    sealed class PowerPlan {public Guid Id;public string Name;public int? Mode;}
    interface IPowerPlanLink {void ApplyMode(int mode);}
    sealed class WindowsPowerPlans : IPowerPlanLink {
        [DllImport("powrprof.dll")] static extern uint PowerEnumerate(IntPtr root,IntPtr scheme,IntPtr subgroup,uint accessor,uint index,byte[] buffer,ref uint size);
        [DllImport("powrprof.dll")] static extern uint PowerReadFriendlyName(IntPtr root,ref Guid scheme,IntPtr subgroup,IntPtr setting,byte[] buffer,ref uint size);
        [DllImport("powrprof.dll")] static extern uint PowerGetActiveScheme(IntPtr root,out IntPtr scheme);
        [DllImport("powrprof.dll")] static extern uint PowerSetActiveScheme(IntPtr root,ref Guid scheme);
        [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr value);
        // OEM scheme identities observed on the target G870; never equate Ultimate/Balanced with these.
        static readonly Guid[] OemIds={new Guid("a9932169-f1fb-4dff-9034-fe1c4ca1886e"),new Guid("653fbb7f-248b-41d0-aa49-03fabbfd5e8e"),new Guid("5da85d85-eb0c-4710-a805-6da8bbcf1f02")};
        static readonly string[] OemNames={"High performance","Gaming","Office"};
        static void Check(uint error){if(error!=0)throw new Win32Exception((int)error);}
        public static int? Identify(Guid id,string name){for(int i=0;i<3;i++)if(id==OemIds[i])return i;
            return null;}
        static string Name(Guid id){uint size=0;uint result=PowerReadFriendlyName(IntPtr.Zero,ref id,IntPtr.Zero,IntPtr.Zero,null,ref size);if(result!=0 && result!=234)Check(result);
            if(size==0 || size>65536)return id.ToString();var data=new byte[size];Check(PowerReadFriendlyName(IntPtr.Zero,ref id,IntPtr.Zero,IntPtr.Zero,data,ref size));return Encoding.Unicode.GetString(data).TrimEnd('\0');}
        public static List<PowerPlan> List(){var plans=new List<PowerPlan>();for(uint i=0;i<1024;i++){
            uint size=16;var buffer=new byte[16];uint result=PowerEnumerate(IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,16,i,buffer,ref size);if(result==259)return plans;Check(result);
            Guid id=new Guid(buffer);string name=Name(id);plans.Add(new PowerPlan{Id=id,Name=name,Mode=Identify(id,name)});
        }throw new IOException("Güç planı sayısı beklenen sınırı aştı.");}
        public static PowerPlan Active(){IntPtr pointer;Check(PowerGetActiveScheme(IntPtr.Zero,out pointer));Guid id;
            try{id=(Guid)Marshal.PtrToStructure(pointer,typeof(Guid));}finally{LocalFree(pointer);}string name=Name(id);return new PowerPlan{Id=id,Name=name,Mode=Identify(id,name)};}
        public static PowerPlan Resolve(int mode,List<PowerPlan> plans){if(mode<0 || mode>2)throw new ArgumentOutOfRangeException("mode");
            foreach(var plan in plans)if(plan.Id==OemIds[mode])return plan;
            throw new IOException("Orijinal "+OemNames[mode]+" güç planı bulunamadı.");}
        static PowerPlan OriginalPlan(int mode){
            if(mode<0 || mode>2)throw new ArgumentOutOfRangeException("mode");
            var plans=List();foreach(var plan in plans)if(plan.Id==OemIds[mode])return plan;
            string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"PowerPlans");
            string file=Path.Combine(directory,new[]{"HighPerformance.pow","Gaming.pow","office.pow"}[mode]);
            if(!File.Exists(file))throw new IOException("Güç planı dosyası bulunamadı. ZIP içindeki PowerPlans klasörünü EXE ile birlikte çıkarın: "+file);
            var info=new FileInfo(file);if(info.Length>2097152)throw new IOException("Üretici güç planı dosyası beklenen boyutta değil.");
            byte[] content=File.ReadAllBytes(file);string text=Encoding.ASCII.GetString(content);
            if(content.Length<4 || text.Substring(0,4)!="regf" || text.IndexOf(OemIds[mode].ToString(),StringComparison.OrdinalIgnoreCase)<0)throw new IOException("Üretici güç planı dosyasının kimliği doğrulanamadı.");
            var start=new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"powercfg.exe"),"/import \""+file+"\" "+OemIds[mode]){UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            using(var process=System.Diagnostics.Process.Start(start)){
                var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
                if(!process.WaitForExit(10000))throw new IOException("Üretici güç planını içe aktarma zaman aşımına uğradı.");
                if(process.ExitCode!=0)throw new IOException("Üretici güç planı yüklenemedi: "+error.GetAwaiter().GetResult()+output.GetAwaiter().GetResult());
            }
            return Resolve(mode,List());
        }
        public void ApplyMode(int mode){var target=OriginalPlan(mode);if(Active().Id==target.Id)return;Guid id=target.Id;Check(PowerSetActiveScheme(IntPtr.Zero,ref id));if(Active().Id!=id)throw new IOException("Windows güç planı geri okunarak doğrulanamadı.");}
    }
}
