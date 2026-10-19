using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;
namespace G870Windows {
    static class StartupService {
        static string Root {get{return ProfileStore.DirectoryPath;}}
        static string Marker {get{return Path.Combine(Root,"startup-choice.txt");}}
        static string Installed {get{return Path.Combine(Root,"App","G870ControlCenter.exe");}}
        static string Sid {get{return WindowsIdentity.GetCurrent().User.Value;}}
        static string TaskName {get{return "G870ControlCenter-"+Sid;}}
        public static bool Enabled {get{return File.Exists(Marker) && File.ReadAllText(Marker).Trim()=="enabled";}}
        public static void Initialize(){
            if(!File.Exists(Marker)){Directory.CreateDirectory(Root);File.WriteAllText(Marker,"disabled");return;}
            if(Enabled)SetEnabled(true);
        }
        static string Hash(string path){using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return Convert.ToBase64String(sha.ComputeHash(file));}
        static void InstallBinary(){
            string source=Application.ExecutablePath;
            if(string.Equals(Path.GetFullPath(source),Path.GetFullPath(Installed),StringComparison.OrdinalIgnoreCase))return;
            Directory.CreateDirectory(Path.GetDirectoryName(Installed));
            string sourcePlans=Path.Combine(Path.GetDirectoryName(source),"PowerPlans");
            string targetPlans=Path.Combine(Path.GetDirectoryName(Installed),"PowerPlans");
            Directory.CreateDirectory(targetPlans);
            foreach(string name in new[]{"HighPerformance.pow","Gaming.pow","office.pow"}){string file=Path.Combine(sourcePlans,name);if(!File.Exists(file))throw new IOException("Başlangıç kurulumu için eksik güç planı: "+file);File.Copy(file,Path.Combine(targetPlans,name),true);}
            if(File.Exists(Installed) && Hash(source)==Hash(Installed))return;
            string temp=Installed+".new";try{File.Copy(source,temp,true);if(File.Exists(Installed))File.Replace(temp,Installed,null);else File.Move(temp,Installed);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public static void SetEnabled(bool enabled){
            Directory.CreateDirectory(Root);
            if(!enabled && !Enabled){File.WriteAllText(Marker,"disabled");return;}
            if(!enabled){Run("/Delete /TN \""+TaskName+"\" /F");File.WriteAllText(Marker,"disabled");return;}
            InstallBinary();
            string xml=Path.Combine(Root,"startup-task.xml");
            try{
                var options=new XmlWriterSettings{Encoding=Encoding.Unicode,Indent=true};
                using(var w=XmlWriter.Create(xml,options)){
                    w.WriteStartDocument();
                    const string ns="http://schemas.microsoft.com/windows/2004/02/mit/task";
                    w.WriteStartElement("Task",ns);w.WriteAttributeString("version","1.2");
                    w.WriteStartElement("Triggers",ns);w.WriteStartElement("LogonTrigger",ns);w.WriteElementString("Enabled",ns,"true");w.WriteElementString("UserId",ns,Sid);w.WriteElementString("Delay",ns,"PT10S");w.WriteEndElement();w.WriteEndElement();
                    w.WriteStartElement("Principals",ns);w.WriteStartElement("Principal",ns);w.WriteAttributeString("id","User");w.WriteElementString("UserId",ns,Sid);w.WriteElementString("LogonType",ns,"InteractiveToken");w.WriteElementString("RunLevel",ns,"HighestAvailable");w.WriteEndElement();w.WriteEndElement();
                    w.WriteStartElement("Settings",ns);w.WriteElementString("MultipleInstancesPolicy",ns,"IgnoreNew");w.WriteElementString("DisallowStartIfOnBatteries",ns,"false");w.WriteElementString("StopIfGoingOnBatteries",ns,"false");w.WriteElementString("StartWhenAvailable",ns,"true");w.WriteElementString("ExecutionTimeLimit",ns,"PT0S");w.WriteEndElement();
                    w.WriteStartElement("Actions",ns);w.WriteAttributeString("Context","User");w.WriteStartElement("Exec",ns);w.WriteElementString("Command",ns,Installed);w.WriteElementString("Arguments",ns,"--startup");w.WriteElementString("WorkingDirectory",ns,Path.GetDirectoryName(Installed));w.WriteEndElement();w.WriteEndElement();w.WriteEndElement();
                }
                Run("/Create /TN \""+TaskName+"\" /XML \""+xml+"\" /F");
                File.WriteAllText(Marker,"enabled");
            }finally{if(File.Exists(xml))File.Delete(xml);}
        }
        static void Run(string arguments){
            var info=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"schtasks.exe"),arguments){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
            using(var p=Process.Start(info)){
                var output=p.StandardOutput.ReadToEndAsync();var errors=p.StandardError.ReadToEndAsync();
                if(!p.WaitForExit(15000))throw new IOException("Windows başlangıç kaydı zaman aşımına uğradı.");
                if(p.ExitCode!=0)throw new IOException("Windows başlangıç kaydı değiştirilemedi: "+errors.GetAwaiter().GetResult()+output.GetAwaiter().GetResult());
            }
        }
    }
}
