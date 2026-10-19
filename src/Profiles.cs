using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace G870Windows {
    public sealed class PointSetting { public int Temperature {get;set;} public int Duty {get;set;} }
    public sealed class Profile {
        public string Fan {get;set;}
        public PointSetting[] Curve {get;set;}
        public bool RgbEnabled {get;set;}
        public bool ZonesEnabled {get;set;}
        public int[] Zones {get;set;}
        public int Color {get;set;}
        public string Effect {get;set;}
        public int Brightness {get;set;}
        public int Interval {get;set;}
        public Profile() {
            Fan="auto"; Effect="1"; Brightness=2; Interval=4; Color=255; Zones=new[]{255,255,255};
            int[] t={45,50,55,60,65,70,75,79}, d={35,35,38,42,47,53,60,70};
            Curve=new PointSetting[8]; for(int i=0;i<8;i++) Curve[i]=new PointSetting{Temperature=t[i],Duty=d[i]};
        }
        public void CopyRgbFrom(Profile source){
            source.Validate();RgbEnabled=source.RgbEnabled;ZonesEnabled=source.ZonesEnabled;
            Color=source.Color;Effect=source.Effect;Brightness=source.Brightness;Interval=source.Interval;
            Zones=(int[])source.Zones.Clone();
        }
        public Profile Copy(){var copy=RgbCopy();copy.Fan=Fan;copy.Curve=new PointSetting[Curve.Length];for(int i=0;i<Curve.Length;i++)copy.Curve[i]=new PointSetting{Temperature=Curve[i].Temperature,Duty=Curve[i].Duty};return copy;}
        public Profile RgbCopy(){var copy=new Profile();copy.CopyRgbFrom(this);return copy;}
        public void Validate() {
            if(Fan!="auto" && Fan!="max" && Fan!="curve") throw new ArgumentException("Fan profili geçersiz.");
            if(Curve==null || Curve.Length!=8) throw new ArgumentException("Sekiz eğri noktası gerekli.");
            for(int i=0;i<8;i++) if(Curve[i]==null || Curve[i].Temperature<20 || Curve[i].Temperature>79 || Curve[i].Duty<35 || Curve[i].Duty>100 ||
                (i>0 && (Curve[i].Temperature<Curve[i-1].Temperature+3 || Curve[i].Duty<Curve[i-1].Duty)))
                throw new ArgumentException("Sıcaklıklar 20–79°C ve en az 3°C aralıklı; fan %35–100 ve azalmayan sırada olmalı.");
            if(Array.IndexOf(Effects.Names,Effect)<0 || Brightness<0 || Brightness>2 || Interval<4 || Interval>12 || Color<0 || Color>0xffffff || Zones==null || Zones.Length!=3)
                throw new ArgumentException("RGB ayarları geçersiz.");
            foreach(int c in Zones) if(c<0 || c>0xffffff) throw new ArgumentException("Bölge rengi geçersiz.");
        }
    }
    public sealed class NamedProfile {
        public string Name {get;set;}
        public int Mode {get;set;}
        public Profile Value {get;set;}
        public void Validate(){
            if(string.IsNullOrWhiteSpace(Name) || Name.Length>64 || Name!=Name.Trim() || Mode<0 || Mode>2 || Value==null)throw new ArgumentException("Özel profil bilgisi geçersiz.");
            foreach(char c in Name)if(char.IsControl(c))throw new ArgumentException("Profil adında kontrol karakteri kullanılamaz.");
            Value.Validate();
        }
        public override string ToString(){return Name;}
    }
    public sealed class Settings {
        public int Version {get;set;}
        public bool Remember {get;set;}
        public Profile LastRgb {get;set;}
        public int LastMode {get;set;}
        public Profile[] Profiles {get;set;}
        public List<NamedProfile> CustomProfiles {get;set;}
        public Settings(){ CustomProfiles=new List<NamedProfile>(); Version=1; LastMode=2; Profiles=new[]{new Profile(),new Profile(),new Profile()}; }
        public void Validate(){
            if(Version!=1 || LastMode<0 || LastMode>2 || Profiles==null || Profiles.Length!=3) throw new ArgumentException("Profil dosyası geçersiz.");
            if(CustomProfiles==null)CustomProfiles=new List<NamedProfile>();
            if(CustomProfiles.Count>100)throw new ArgumentException("En fazla 100 özel profil saklanabilir.");
            var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var custom in CustomProfiles){if(custom==null)throw new ArgumentException("Boş özel profil.");custom.Validate();if(!names.Add(custom.Name))throw new ArgumentException("Tekrarlanan özel profil adı.");}
            if(LastRgb!=null)LastRgb.Validate();
            foreach(var p in Profiles) { if(p==null)throw new ArgumentException("Eksik profil.");p.Validate(); }
        }
    }
    static class ProfileStore {
        public static readonly string DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"G870ControlCenterWindows");
        public static readonly string FilePath=Path.Combine(DirectoryPath,"profiles-v1.json");
        public static Settings Read(string path){ var value=new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(path));if(value==null)throw new IOException("Boş profil dosyası.");value.Validate();return value; }
        public static void Write(string path,Settings value){
            value.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{ File.WriteAllText(temp,new JavaScriptSerializer().Serialize(value));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path); }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
    static class CurvePolicy {
        public static int Interpolate(PointSetting[] points,int temperature){
            if(temperature<=points[0].Temperature)return points[0].Duty;
            for(int i=1;i<8;i++)if(temperature<=points[i].Temperature){int span=points[i].Temperature-points[i-1].Temperature;
                return points[i-1].Duty+((points[i].Duty-points[i-1].Duty)*(temperature-points[i-1].Temperature)+span-1)/span;}
            return points[7].Duty;
        }
        public static int Decide(Profile p,Snapshot s,int previous,int grace){
            p.Validate();
            if(s.Cpu<5 || s.Cpu>110 || s.Cpu>=80 || (s.GpuAvailable && (s.Gpu<5 || s.Gpu>110 || s.Gpu>=80)) || (grace>=4 && (s.CpuRpm<300 || (s.GpuAvailable && s.GpuRpm<300))))return 255;
            int temperature=(int)Math.Max(s.Cpu,s.Gpu), duty=Interpolate(p.Curve,temperature);
            if(previous>=35 && previous<=100 && duty<previous && Interpolate(p.Curve,temperature+3)>=previous)duty=previous;
            return duty;
        }
    }
    static class Effects {
        public static readonly string[] Names={"1","3","6","7","rainbow","pastel","police","candle","pulse","thermal"};
        public static uint NativeMode(string effect){uint n;return uint.TryParse(effect,out n)?n:1;}
        static int Hsv(double h,double saturation){
            double x=h*6;int sector=(int)x;double f=x-sector, p=1-saturation,q=1-f*saturation,t=1-(1-f)*saturation;
            double[] r={1,q,p,p,t,1},g={t,1,1,q,p,p},b={p,p,t,1,1,q};int i=sector%6;
            return ((int)Math.Round(r[i]*255)<<16)|((int)Math.Round(g[i]*255)<<8)|(int)Math.Round(b[i]*255);
        }
        public static int ColorFor(Profile p,uint temperature,int step){
            switch(p.Effect){
                case "rainbow":return Hsv((step%12)/12.0,1);
                case "pastel":return Hsv((step%12)/12.0,0.35);
                case "police":return step%2==0?0xff0000:0x0000ff;
                case "candle":return 0xff0003|((int)(46+(Math.Sin(step*2.3)+1)*38)<<8);
                case "thermal":return Hsv((1-Math.Max(0,Math.Min(1,(temperature-40.0)/40)))*0.66,1);
                case "pulse":double level=new[]{0.15,0.35,0.65,1,0.65,0.35}[step%6];return ((int)Math.Round(((p.Color>>16)&255)*level)<<16)|((int)Math.Round(((p.Color>>8)&255)*level)<<8)|(int)Math.Round((p.Color&255)*level);
                default:return p.Color;
            }
        }
    }
}
