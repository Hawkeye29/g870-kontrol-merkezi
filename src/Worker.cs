using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace G870Windows {
    sealed class Command { public string Action {get;set;} public int Mode {get;set;} public Profile Profile {get;set;} }
    sealed class Reply { public bool Ok; public bool RgbSoftwareEffect; public MuxState Mux; public string Message; public Snapshot Status; public string Fan; public int Duty; public bool RestorePending; }
    sealed class Engine {
        readonly Controller controller;
        readonly IPowerPlanLink powerPlans;
        Profile active;
        int mode, previous=255, grace, step;
        bool fanOwned;
        DateTime nextRgb=DateTime.MinValue;
        public string Message="Hazır.";
        public Engine(Controller c,IPowerPlanLink power=null){controller=c;powerPlans=power;}
        public Reply Read(){uint ignoredEffect;var s=controller.Read();return new Reply{Ok=true,RgbSoftwareEffect=active!=null && active.RgbEnabled && !active.ZonesEnabled && !uint.TryParse(active.Effect,out ignoredEffect),Status=s,Message=Message,Fan=active==null?(fanOwned?"restore-pending":"auto"):active.Fan,Duty=previous,RestorePending=active==null && fanOwned};}
        public void Restore(){
            active=null;
            if(fanOwned){controller.SetFan(255);fanOwned=false;previous=255;}
        }
                uint requestedMux;
        public MuxState ReadMux(){var state=controller.ReadMux();state.Requested=requestedMux;return state;}
        public MuxState SetMux(int value,bool demo){
            if(value<1 || value>3)throw new ArgumentOutOfRangeException("value");
            var state=ReadMux();
            if(!state.Supported)throw new IOException("MUX desteği firmware cevabında doğrulanamadı.");
            if(value==3 && state.ExternalDisplay!=0)throw new IOException("Dahili GPU moduna geçmeden önce harici ekranı çıkarın.");
            if(!demo)MuxSafety.CheckEncryption();
            Restore();controller.SetMux((uint)value);requestedMux=(uint)value;
            Message="MUX komutu gönderildi. Windows'u yeniden başlatın; açılıştan sonra modu kontrol edin.";
            state.Requested=requestedMux;return state;
        }
        public void Automatic(){
            fanOwned=true;
            try{controller.SetFan(255);fanOwned=false;previous=255;if(active!=null)active.Fan="auto";Message="Fan BIOS otomatik.";}
            catch{active=null;throw;}
        }
        public void ApplyRgb(int selectedMode,Profile rgb){
            if(rgb==null)throw new ArgumentException("RGB ayarları eksik.");rgb.Validate();
            var snapshot=controller.Read();
            if(snapshot.Mode!=selectedMode)throw new IOException("Önce düzenlediğiniz performans modunu seçin; RGB bu mod için kaydedilecek.");
            if(active==null && fanOwned)throw new IOException("BIOS'a dönüş bekleniyor; RGB kontrolü geçici olarak kapalı.");
            if(active==null){active=new Profile();mode=(int)snapshot.Mode;}
            // Update only illumination; preserve the running fan policy and its watchdog counters.
            active.CopyRgbFrom(rgb);step=0;nextRgb=DateTime.MaxValue;
            try{
                controller.SetRgb(active,Math.Max(snapshot.Cpu,snapshot.Gpu),step++);
                uint native;
                if(!active.ZonesEnabled && !uint.TryParse(active.Effect,out native))nextRgb=DateTime.UtcNow.AddSeconds(active.Interval);
                Message="RGB uygulandı; fan ve performans ayarları korundu.";
            }catch{active.RgbEnabled=false;throw;}
        }
        public void StopRgbEffect(){if(active!=null)active.RgbEnabled=false;nextRgb=DateTime.MaxValue;Message="Yazılım efekti durduruldu; son renk klavyede kalır.";}
        public void Apply(int selectedMode,Profile profile){
            if(selectedMode<0 || selectedMode>2 || profile==null)throw new ArgumentException("Profil geçersiz.");
            profile.Validate(); Restore();
            try {
                controller.SetMode((uint)selectedMode); mode=selectedMode;
                var s=controller.Read();
                if(profile.Fan=="curve" && CurvePolicy.Decide(profile,s,255,0)==255)throw new IOException("Sıcaklık/sensör koşulları eğriyi başlatmaya uygun değil.");
                active=profile;grace=0;previous=255;step=0;nextRgb=DateTime.MinValue;
                if(profile.Fan=="max"){fanOwned=true;controller.SetFan(100);previous=100;}
                Tick(); if(active==null)throw new IOException(Message);if(powerPlans!=null)powerPlans.ApplyMode(selectedMode); Message="Profil uygulandı.";
            } catch {Restore();throw;}
        }
        public void Tick(){
            if(active==null){if(fanOwned)Restore();return;}
            try {
                var s=controller.Read();
                if(s.Mode!=mode){Restore();Message="Mod dışarıdan değişti; fan BIOS'a döndü.";return;}
                if(active.Fan=="curve"){
                    int duty=CurvePolicy.Decide(active,s,previous,grace);
                    if(duty==255){Restore();Message="Sıcaklık/sensör/fan koruması: BIOS otomatik. Eğriyi yeniden uygulayın.";return;}
                    if(duty!=previous){fanOwned=true;controller.SetFan((uint)duty);previous=duty;}
                    grace++;
                }
                if(active.RgbEnabled && DateTime.UtcNow>=nextRgb){
                    controller.SetRgb(active,Math.Max(s.Cpu,s.Gpu),step++);
                    nextRgb=DateTime.UtcNow.AddSeconds(active.Interval);
                    // Native effects and zone colors only need one write per application.
                    if(active.ZonesEnabled || Array.IndexOf(new[]{"1","3","6","7"},active.Effect)>=0)nextRgb=DateTime.MaxValue;
                }
            } catch(Exception ex){
                active=null;
                try{Restore();}catch(Exception restore){Message="Bağlantı hatası; BIOS'a dönüş bekliyor: "+restore.Message;throw new IOException(Message,restore);}
                Message="Kontrol durduruldu, fan BIOS'a döndü: "+ex.Message;
                throw;
            }
        }
    }
    static class Worker {
        public static void Run(int parentId,long parentStart,bool demo){
            var queue=new BlockingCollection<string>();var json=new JavaScriptSerializer();
            var engine=new Engine(new Controller(demo?(ITransport)new DemoTransport():new WmiTransport()),demo?null:new WindowsPowerPlans());
            var heartbeat=Stopwatch.StartNew();bool end=false;
            Task.Run(()=>{try{string line;while((line=Console.ReadLine())!=null)queue.Add(line);}finally{queue.CompleteAdding();}});
            try{
                using(var parent=Process.GetProcessById(parentId)){
                    if(parent.StartTime.ToUniversalTime().Ticks!=parentStart)return;
                    while(!end){
                        if(parent.HasExited || heartbeat.Elapsed.TotalSeconds>12 || queue.IsCompleted)break;
                        string line;
                        if(queue.TryTake(out line,1000)){
                            heartbeat.Restart();Reply reply;
                            try{
                                var command=json.Deserialize<Command>(line);MuxState muxResult=null;
                                if(command.Action=="apply")engine.Apply(command.Mode,command.Profile);
                                else if(command.Action=="rgb")engine.ApplyRgb(command.Mode,command.Profile);
                                else if(command.Action=="rgbstop")engine.StopRgbEffect();
                                else if(command.Action=="auto")engine.Automatic();
                                else if(command.Action=="muxread"){}
                                else if(command.Action=="muxset")muxResult=engine.SetMux(command.Mode,demo);
                                else if(command.Action=="stop"){engine.Restore();end=true;}
                                else if(command.Action!="read")throw new ArgumentException("Bilinmeyen işlem.");
                                reply=end?new Reply{Ok=true,Message="Fan BIOS'a döndü."}:(command.Action=="rgb" || command.Action=="rgbstop" || command.Action=="muxset")?new Reply{Ok=true,Message=engine.Message,Mux=muxResult}:engine.Read();
                                if(command.Action=="muxread")reply.Mux=engine.ReadMux();
                            }catch(Exception ex){reply=new Reply{Ok=false,Message=ex.Message};}
                            Console.WriteLine(json.Serialize(reply));Console.Out.Flush();
                        }
                        try{engine.Tick();}catch{/* Next command reports state; recovery retries on the next tick. */}
                    }
                }
            } finally {
                // Independent process survives a UI crash and retries BIOS restoration.
                for(int i=0;i<5;i++)try{engine.Restore();break;}catch{System.Threading.Thread.Sleep(1000);}
            }
        }
    }
    sealed class WorkerClient : IDisposable {
        readonly Process process;
        readonly JavaScriptSerializer json=new JavaScriptSerializer();
        readonly System.Threading.SemaphoreSlim gate=new System.Threading.SemaphoreSlim(1,1);
        bool failed,stopped;
        public bool Broken {get{return failed || process.HasExited;}}
        public WorkerClient(bool demo){
            using(var parent=Process.GetCurrentProcess()){
                var info=new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath,
                    "--worker "+parent.Id+" "+parent.StartTime.ToUniversalTime().Ticks+(demo?" --demo":"")){
                    UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true
                };
                process=Process.Start(info);
            }
        }
        public async Task<Reply> Send(Command command){
            await gate.WaitAsync();
            try{
                if(failed || stopped)throw new IOException("Kontrol bağlantısı kapalı. Uygulamayı yeniden başlatın.");
                await process.StandardInput.WriteLineAsync(json.Serialize(command));await process.StandardInput.FlushAsync();
                var read=process.StandardOutput.ReadLineAsync();
                if(await Task.WhenAny(read,Task.Delay(10000))!=read){failed=true;process.StandardInput.Close();throw new IOException("WMI yanıtı zaman aşımına uğradı; gözetim süreci BIOS'a dönüşü deneyecek.");}
                string line=await read;if(line==null)throw new IOException("Gözetim süreci kapandı.");
                var reply=json.Deserialize<Reply>(line);if(reply==null || !reply.Ok)throw new IOException(reply==null?"Geçersiz cevap.":reply.Message);
                if(command.Action=="stop")stopped=true;
                return reply;
            }finally{gate.Release();}
        }
        public void Dispose(){try{process.StandardInput.Close();}catch{}process.Dispose();}
    }
}
