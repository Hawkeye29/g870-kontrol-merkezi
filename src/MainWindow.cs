using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace G870Windows {
    static class Mac {
        public static readonly bool Dark=IsDark();
        static bool IsDark(){try{using(var k=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))return k!=null && Convert.ToInt32(k.GetValue("AppsUseLightTheme",1))==0;}catch{return false;}}
        public static readonly Color Background=Dark?Color.FromArgb(30,30,32):Color.FromArgb(242,242,247);
        public static readonly Color Card=Dark?Color.FromArgb(43,43,46):Color.White;
        public static readonly Color Text=Dark?Color.FromArgb(242,242,247):Color.FromArgb(30,30,35);
        public static readonly Color Secondary=Dark?Color.FromArgb(190,190,200):Color.FromArgb(88,88,98);
        public static readonly Color Line=Dark?Color.FromArgb(65,65,70):Color.FromArgb(229,229,235);
        public static readonly Color Cyan=Color.FromArgb(28,177,213),Orange=Color.FromArgb(244,148,45),Blue=Color.FromArgb(0,122,255);
        public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static Label Label(string text,float size=11,bool bold=false){return new Label{Text=text,ForeColor=Text,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};}
    }
    class MacCard : Panel {
        public MacCard(){DoubleBuffered=true;BackColor=Mac.Background;Padding=new Padding(20);}
        protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Mac.Background);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Mac.Round(new RectangleF(0,0,Width-1,Height-1),20))using(var brush=new SolidBrush(Mac.Card))e.Graphics.FillPath(brush,path);}
    }
    static class FocusStyle {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
        public static void Hide(Control control){if(control.IsHandleCreated)SendMessage(control.Handle,0x0128,new IntPtr(0x00010001),IntPtr.Zero);}
    }
    sealed class CleanCheckBox : CheckBox {
        protected override bool ShowFocusCues { get { return false; } }
        protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x0007 || m.Msg==0x0127)FocusStyle.Hide(this);}
    }

    sealed class CleanComboBox : ComboBox {
        protected override bool ShowFocusCues { get { return false; } }
        protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x0007 || m.Msg==0x0127)FocusStyle.Hide(this);}
    }

    sealed class CleanTrackBar : TrackBar {
        protected override bool ShowFocusCues { get { return false; } }
        protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x0007 || m.Msg==0x0127)FocusStyle.Hide(this);}
    }

    sealed class CleanNumericUpDown : NumericUpDown {
        protected override bool ShowFocusCues { get { return false; } }
        protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x0007 || m.Msg==0x0127)FocusStyle.Hide(this);}
    }

    sealed class MacButton : Button {
        protected override bool ShowFocusCues { get { return false; } }
        protected override void WndProc(ref Message m){base.WndProc(ref m);if(m.Msg==0x0007 || m.Msg==0x0127)FocusStyle.Hide(this);}
        bool selected;
        public bool Selected{get{return selected;}set{selected=value;Invalidate();}}
        public bool Primary;public bool UseSwatch;public bool HideFocusRing=true;
        public MacButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Height=31;AutoSize=false;Width=112;Anchor=AnchorStyles.Right|AnchorStyles.Top;Cursor=Cursors.Hand;Margin=new Padding(4,0,0,0);BackColor=Mac.Card;ForeColor=Mac.Text;Font=new Font("Segoe UI",10);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?Mac.Card:Parent.BackColor);
            Color fill=UseSwatch?BackColor:Primary?Mac.Blue:Selected?Color.FromArgb(Mac.Dark?70:27,Mac.Cyan):Mac.Dark?Color.FromArgb(64,64,69):Color.FromArgb(243,243,246);
            using(var path=Mac.Round(new RectangleF(0,0,Width-1,Height-1),7))using(var b=new SolidBrush(fill))g.FillPath(b,path);
            TextRenderer.DrawText(g,Text,Font,ClientRectangle,!Enabled?Mac.Secondary:UseSwatch?ForeColor:Primary?Color.White:Selected?Mac.Cyan:Mac.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);

        }
    }
    sealed class SensorCard : MacCard {
        public string Title;public Color Accent;uint? temperature,rpm;
        public SensorCard(string title,Color accent){Title=title;Accent=accent;}
        public void Set(uint t,uint r){temperature=t;rpm=r;Invalidate();}
        public void Clear(){temperature=null;rpm=null;Invalidate();}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;
            using(var titleFont=new Font("Segoe UI",11,FontStyle.Bold))using(var number=new Font("Segoe UI",54,FontStyle.Regular))using(var unit=new Font("Segoe UI",18))using(var small=new Font("Segoe UI",10))
            using(var ink=new SolidBrush(Mac.Text))using(var muted=new SolidBrush(Mac.Secondary))using(var accent=new SolidBrush(Accent)){
                g.DrawString(Title,titleFont,accent,24,20);string value=temperature.HasValue?temperature.Value.ToString():"—";
                g.DrawString(value,number,ink,18,48);float width=g.MeasureString(value,number).Width;g.DrawString("°C",unit,muted,19+width,89);
                using(var rail=new SolidBrush(Mac.Line))g.FillRectangle(rail,24,153,Width-48,5);g.FillRectangle(accent,24,153,(Width-48)*Math.Min(temperature??0,100)/100f,5);
                g.DrawString("Fan",small,muted,24,179);string fan=rpm.HasValue?rpm+" RPM":"— RPM";g.DrawString(fan,small,muted,Width-24-g.MeasureString(fan,small).Width,179);
            }
        }
    }
    sealed class CurveGraph : Panel {
        public PointSetting[] Points;
        public CurveGraph(){DoubleBuffered=true;BackColor=Mac.Card;}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Points==null)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            int w=Width-10,h=Height-24;using(var pen=new Pen(Mac.Line)){pen.DashStyle=DashStyle.Dash;for(int i=0;i<5;i++)g.DrawLine(pen,5,i*h/4,w,i*h/4);}
            bool valid=true;for(int i=1;i<8;i++)if(Points[i].Temperature<Points[i-1].Temperature+3 || Points[i].Duty<Points[i-1].Duty)valid=false;
            Color color=valid?Color.FromArgb(52,176,92):Mac.Orange;var points=new PointF[10];points[0]=new PointF(5,(100-Points[0].Duty)*h/100f);
            for(int i=0;i<8;i++)points[i+1]=new PointF(5+(Points[i].Temperature-20)*(w-5)/60f,(100-Points[i].Duty)*h/100f);points[9]=new PointF(w,points[8].Y);
            using(var pen=new Pen(color,2))g.DrawLines(pen,points);using(var b=new SolidBrush(color))for(int i=1;i<9;i++)g.FillEllipse(b,points[i].X-4,points[i].Y-4,8,8);
            using(var b=new SolidBrush(Mac.Secondary)){g.DrawString("20°C",Font,b,0,Height-20);g.DrawString("Fan isteği · %0–100",Font,b,Width/2-65,Height-20);g.DrawString("80°C",Font,b,Width-40,Height-20);}
        }
    }
    sealed class TemperatureHistory : Panel {
        readonly System.Collections.Generic.List<PointF> samples=new System.Collections.Generic.List<PointF>();
        public TemperatureHistory(){DoubleBuffered=true;BackColor=Mac.Card;}
        public void Add(uint cpu,uint gpu){samples.Add(new PointF(cpu,gpu));if(samples.Count>60)samples.RemoveAt(0);Invalidate();}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(Mac.Line)){pen.DashStyle=DashStyle.Dash;for(int i=0;i<5;i++)g.DrawLine(pen,0,i*(Height-20)/4,Width,i*(Height-20)/4);}
            if(samples.Count<2)return;for(int channel=0;channel<2;channel++){var line=new PointF[samples.Count];for(int i=0;i<line.Length;i++)line[i]=new PointF(i*(Width-1f)/(line.Length-1),(1-Math.Min(channel==0?samples[i].X:samples[i].Y,100)/100)*(Height-20));using(var pen=new Pen(channel==0?Mac.Cyan:Mac.Orange,2))g.DrawLines(pen,line);}
        }
    }
    sealed class MainWindow : Form {
        readonly bool demo;WorkerClient client;
        readonly Label powerLabel=Mac.Label("Windows güç planı: okunuyor…",9);
        readonly Label muxLabel=Mac.Label("MUX durumu okunuyor…",10);
        readonly FlowLayoutPanel muxActions=new FlowLayoutPanel();
        bool muxInitialized;
        readonly NotifyIcon tray=new NotifyIcon();
        Icon brandedIcon;
        TrayPopup trayPopup;
        readonly ToolStripMenuItem startupItem=new ToolStripMenuItem("Windows açılışında başlat");
        bool exitRequested,startInTray;Settings settings=new Settings();
        readonly Label feedback=Mac.Label(""),link=Mac.Label("●  Veri akışı bekleniyor",9),fanStatus=Mac.Label("BIOS yönetimi",9),footer=Mac.Label("BIOS modu: —",10),profileCaption=Mac.Label("",9);
        readonly ComboBox mode=new CleanComboBox(),fan=new CleanComboBox(),effect=new CleanComboBox();
        readonly NumericUpDown brightness=new CleanNumericUpDown(),interval=new CleanNumericUpDown();
        readonly CheckBox rgb=new CleanCheckBox(),zones=new CleanCheckBox(),remember=new CleanCheckBox(),autoStart=new CleanCheckBox();
        DateTime nextRestoreAttempt=DateTime.MinValue;
        readonly NumericUpDown[] temperatures=new NumericUpDown[8],duties=new NumericUpDown[8];readonly Button[] colorButtons=new Button[4];readonly MacButton[] modeButtons=new MacButton[3];
        readonly Label rgbStatus=Mac.Label("Henüz bir RGB ayarı uygulanmadı.",9);
        MacButton rgbApply,rgbStop;
        readonly CurveGraph graph=new CurveGraph();readonly TemperatureHistory history=new TemperatureHistory();
        readonly SensorCard cpu=new SensorCard("CPU",Mac.Cyan),gpu=new SensorCard("GPU",Mac.Orange);
        readonly MacCard controlCard=new MacCard();
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();bool busy,closing,loading,connected,startupDone;int selected=2;
        public MainWindow(bool simulation,bool minimized=false,EventWaitHandle activation=null){
            startInTray=minimized;
            demo=simulation;Text="G870 Kontrol Merkezi"+(demo?" · Demo":"");ClientSize=new Size(830,700);MinimumSize=new Size(800,650);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;Font=new Font("Segoe UI",10);BackColor=Mac.Background;ForeColor=Mac.Text;
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Mac.Background};
            var body=new TableLayoutPanel{ColumnCount=1,RowCount=0,AutoSize=false,Height=56,Padding=new Padding(28),Margin=Padding.Empty,BackColor=Mac.Background};body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(body);Controls.Add(scroll);
            scroll.ClientSizeChanged+=(s,e)=>FitContent(scroll,body);
            FitContent(scroll,body);
            var header=new TableLayoutPanel{ColumnCount=2,Dock=DockStyle.Fill,BackColor=Mac.Background};header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,70));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));
            var branding=new TableLayoutPanel{ColumnCount=1,RowCount=3,Dock=DockStyle.Fill};branding.RowStyles.Add(new RowStyle(SizeType.Absolute,24));branding.RowStyles.Add(new RowStyle(SizeType.Absolute,44));branding.RowStyles.Add(new RowStyle(SizeType.Absolute,24));
            var brand=Mac.Label("E X C A L I B U R",9,true);brand.ForeColor=Mac.Secondary;branding.Controls.Add(brand,0,0);branding.Controls.Add(Mac.Label("Control Center",27,true),0,1);var model=Mac.Label("G870 · 1362-BV70X-C",10);model.ForeColor=Mac.Secondary;branding.Controls.Add(model,0,2);header.Controls.Add(branding,0,0);link.TextAlign=ContentAlignment.TopRight;link.Padding=new Padding(0,13,0,0);link.ForeColor=Mac.Secondary;header.Controls.Add(link,1,0);Add(body,header,100);
            var sensors=new TableLayoutPanel{ColumnCount=2,Dock=DockStyle.Fill,Margin=Padding.Empty};sensors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));sensors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));cpu.Dock=gpu.Dock=DockStyle.Fill;cpu.Margin=new Padding(0,0,9,0);gpu.Margin=new Padding(9,0,0,0);sensors.Controls.Add(cpu,0,0);sensors.Controls.Add(gpu,1,0);Add(body,sensors,230);
            BuildControls();Add(body,controlCard,305);var startupCard=new MacCard();BuildStartup(startupCard);Add(body,startupCard,185);var muxCard=new MacCard();BuildMux(muxCard);Add(body,muxCard,170);
            var curve=new MacCard();BuildCurve(curve);Add(body,curve,580);
            var color=new MacCard();BuildRgb(color);Add(body,color,385);
            var chart=new MacCard();var chartGrid=Grid(chart,2);chartGrid.RowStyles.Add(new RowStyle(SizeType.Absolute,38));chartGrid.RowStyles.Add(new RowStyle(SizeType.Percent,100));chartGrid.Controls.Add(Mac.Label("Sıcaklık geçmişi                     CPU    GPU    ·    Son 60 ölçüm",10,true),0,0);history.Dock=DockStyle.Fill;chartGrid.Controls.Add(history,0,1);Add(body,chart,190);
            Add(body,footer,40);
            mode.Items.AddRange(new object[]{"Yüksek performans","Oyun","Ofis"});fan.Items.AddRange(new object[]{"auto","max","curve"});
            try{if(File.Exists(ProfileStore.FilePath))settings=ProfileStore.Read(ProfileStore.FilePath);}catch(Exception ex){feedback.Text="Profil dosyası okunamadı: "+ex.Message;}
            selected=settings.LastMode;mode.SelectedIndex=selected;remember.Checked=settings.Remember;autoStart.Checked=!demo && StartupService.Enabled;autoStart.Enabled=!demo;LoadEditor();if(settings.LastRgb!=null)LoadRgb(settings.LastRgb);UpdateCaption();
            mode.SelectedIndexChanged+=(s,e)=>{if(loading)return;selected=mode.SelectedIndex;LoadEditor();UpdateCaption();};
            remember.CheckedChanged+=(s,e)=>{
                if(loading)return;bool previous=settings.Remember;
                try{settings.Remember=remember.Checked;ProfileStore.Write(ProfileStore.FilePath,settings);feedback.Text=remember.Checked?"Son ayarlar uygulamanın bir sonraki açılışında yüklenecek.":"Açılışta ayar uygulama kapalı. Windows başlangıç seçeneği değişmedi.";}
                catch(Exception ex){settings.Remember=previous;loading=true;remember.Checked=previous;loading=false;feedback.Text="Tercih kaydedilemedi: "+ex.Message;}
            };
            autoStart.CheckedChanged+=async(s,e)=>{if(!loading && !demo)await UpdateWindowsStartup(autoStart.Checked);};
            Shown+=async(s,e)=>{autoStart.Enabled=startupItem.Enabled=false;FitContent(scroll,body);scroll.AutoScrollPosition=Point.Empty;if(startInTray)HideToTray();try{client=new WorkerClient(demo);await RefreshStatus();timer.Start();if(!demo){try{await Task.Run(()=>StartupService.Initialize());SyncStartupControls();}catch(Exception ex){feedback.Text="Otomatik başlatma kurulamadı: "+ex.Message;tray.ShowBalloonTip(6000,"G870 · Otomatik başlatma", "Başlangıç kaydı kurulamadı. Pencereyi açıp hata ayrıntısını kontrol edin.",ToolTipIcon.Warning);}}}catch(Exception ex){feedback.Text=ex.Message;}finally{autoStart.Enabled=startupItem.Enabled=!demo;}};
            LoadBrandIcon();ConfigureTray();
            if(activation!=null){
                var activationTimer=new System.Windows.Forms.Timer{Interval=200};
                activationTimer.Tick+=(s,e)=>{if(activation.WaitOne(0) && !closing){startInTray=false;OpenWindow();}};
                Shown+=(s,e)=>activationTimer.Start();
                FormClosed+=(s,e)=>{activationTimer.Stop();activationTimer.Dispose();};
            }
            if(startInTray){WindowState=FormWindowState.Minimized;ShowInTaskbar=false;}
            Resize+=(s,e)=>{if(WindowState==FormWindowState.Minimized)HideToTray();};
            timer.Interval=2000;timer.Tick+=async(s,e)=>await RefreshStatus();FormClosing+=OnClosing;
        }
        void BuildMux(MacCard card){
            var grid=Grid(card,3);grid.RowStyles.Add(new RowStyle(SizeType.Absolute,35));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,47));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            grid.Controls.Add(Mac.Label("MUX Switch · Ekran kartı modu",11,true),0,0);muxLabel.ForeColor=Mac.Secondary;grid.Controls.Add(muxLabel,0,1);
            muxActions.Dock=DockStyle.Fill;muxActions.BackColor=Mac.Card;muxActions.Enabled=false;
            var hybrid=Button("Hibrit",async()=>await ChangeMux(1));var discrete=Button("Yalnızca NVIDIA",async()=>await ChangeMux(2));discrete.Width=150;
            muxActions.Controls.Add(hybrid);muxActions.Controls.Add(discrete);var internalGpu=Button("Yalnızca dahili",async()=>await ChangeMux(3));internalGpu.Width=145;muxActions.Controls.Add(internalGpu);muxActions.Controls.Add(Button("Durumu yenile",async()=>await Execute(async()=>{var reply=await client.Send(new Command{Action="muxread"});ShowMux(reply.Mux);})));grid.Controls.Add(muxActions,0,2);
        }
        void ShowMux(MuxState state){
            if(state==null)return;string name=state.Current==1?"Hibrit":state.Current==2?"Yalnızca NVIDIA":state.Current==3?"Yalnızca dahili GPU":"Bilinmiyor";
            muxLabel.Text=state.Supported?"Mevcut: "+name+". "+(state.Requested!=0?"İstek gönderildi; yeniden başlatınca doğrulayın.":"Değişiklik yeniden başlatma gerektirir."):state.Message;
            muxActions.Controls[0].Enabled=muxActions.Controls[1].Enabled=state.Supported;muxActions.Controls[2].Enabled=state.Supported && state.ExternalDisplay==0;
        }
        void MuxLog(string message){try{Directory.CreateDirectory(ProfileStore.DirectoryPath);File.AppendAllText(Path.Combine(ProfileStore.DirectoryPath,"mux-restart.log"),DateTimeOffset.Now.ToString("o")+" "+message+Environment.NewLine);}catch{}}
        async Task ChangeMux(int target){
            if(busy){MessageBox.Show(this,"Bir işlem sürüyor. Tamamlandıktan sonra tekrar deneyin.","MUX Switch");return;}
            if(!connected){MessageBox.Show(this,"Donanım bağlantısı yok. MUX değişikliği gönderilmedi.","MUX Switch");return;}
            string name=target==1?"Hibrit":target==2?"Yalnızca NVIDIA":"Yalnızca dahili GPU";
            if(MessageBox.Show(this,name+" modu seçildi.\n\nBilgisayar yeniden başlatılacak. Devam edilsin mi?\nÖnce açık çalışmalarınızı kaydedin.","MUX Switch · Yeniden başlatma",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            await Execute(async()=>{
                string stage="MUX komutu";bool muxSent=false;
                try{
                    MuxLog("START target="+target+" demo="+demo);
                    var reply=await client.Send(new Command{Action="muxset",Mode=target});muxSent=true;ShowMux(reply.Mux);MuxLog("MUX_ACK");
                    if(demo){feedback.Text="Demo: yeniden başlatma yapılmaz.";MessageBox.Show(this,feedback.Text,"MUX · Demo");MuxLog("DEMO_NO_RESTART");return;}
                    stage="Windows yeniden başlatma";feedback.Text="MUX komutu gönderildi. Windows yeniden başlatılıyor…";
                    await Task.Run(()=>{
                        var info=new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"shutdown.exe"),"/r /t 0"){
                            UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true
                        };
                        using(var process=System.Diagnostics.Process.Start(info)){
                            var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
                            if(!process.WaitForExit(10000))throw new IOException("shutdown.exe yanıtı zaman aşımına uğradı.");
                            string details=output.GetAwaiter().GetResult()+error.GetAwaiter().GetResult();MuxLog("SHUTDOWN exit="+process.ExitCode+" "+details);
                            if(process.ExitCode!=0)throw new IOException("Windows hata kodu: "+process.ExitCode+"\n"+details);
                        }
                    });
                    feedback.Text="Windows yeniden başlatma isteğini kabul etti.";
                }catch(Exception ex){
                    MuxLog("ERROR stage="+stage+" "+ex.ToString());
                    string report=Path.Combine(ProfileStore.DirectoryPath,"mux-restart.log");
                    feedback.Text=stage+" başarısız: "+ex.Message;
                    MessageBox.Show(this,(muxSent?"MUX komutu gönderildi, ancak yeniden başlatma tamamlanamadı.":"MUX onayı alınamadı; otomatik yeniden başlatma yapılmadı.")+"\n\n"+ex.Message+"\n\nHata kaydı: "+report,"MUX · İşlem tamamlanamadı",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                }
            });
        }
        void BuildStartup(MacCard card){
            var grid=Grid(card,4);int[] heights={35,34,34,38};foreach(int h in heights)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            grid.Controls.Add(Mac.Label("Başlangıç seçenekleri",11,true),0,0);
            remember.Text="Son ayarlarla başlat";remember.Dock=DockStyle.Fill;remember.ForeColor=Mac.Text;grid.Controls.Add(remember,0,1);
            autoStart.Text="Windows açılışında uygulamayı başlat";autoStart.Dock=DockStyle.Fill;autoStart.ForeColor=Mac.Text;grid.Controls.Add(autoStart,0,2);
            var hint=Mac.Label("İki seçenek bağımsızdır. Son kaydedilen fan, RGB ve performans profili geri yüklenir.",9);hint.ForeColor=Mac.Secondary;grid.Controls.Add(hint,0,3);
        }
        void SyncStartupControls(){loading=true;autoStart.Checked=!demo && StartupService.Enabled;startupItem.Checked=autoStart.Checked;loading=false;}
        async Task UpdateWindowsStartup(bool enabled){
            if(demo)return;autoStart.Enabled=startupItem.Enabled=false;
            try{await Task.Run(()=>StartupService.SetEnabled(enabled));SyncStartupControls();feedback.Text=enabled?"Windows'a girişte uygulama açılacak. Son ayarları yükleme tercihi bağımsızdır.":"Windows açılışında başlatma kapalı. Son ayarları yükleme tercihi değişmedi.";}
            catch(Exception ex){SyncStartupControls();feedback.Text="Windows başlangıç tercihi değiştirilemedi: "+ex.Message;MessageBox.Show(this,feedback.Text,"Windows başlangıcı",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
            finally{autoStart.Enabled=startupItem.Enabled=true;}
        }
        protected override void Dispose(bool disposing){
            // Keep the icon alive until WinForms has finished destroying window and tray handles.
            base.Dispose(disposing);
            if(disposing && brandedIcon!=null){brandedIcon.Dispose();brandedIcon=null;}
        }
        void LoadBrandIcon(){
            using(var stream=typeof(MainWindow).Assembly.GetManifestResourceStream("G870Windows.AppIcon")){
                if(stream==null)return;
                using(var source=new Icon(stream)){brandedIcon=(Icon)source.Clone();Icon=brandedIcon;}
            }
            
        }
        async void RequestExit(){exitRequested=true;while(busy)await Task.Delay(200);Close();}
        void ConfigureTray(){
            tray.Icon=Icon??SystemIcons.Application;tray.Text="G870 Kontrol Merkezi";tray.Visible=true;
            trayPopup=new TrayPopup(Icon,()=>OpenWindow(),()=>RequestExit(),async target=>{
                if(busy || !connected)return;trayPopup.Hide();selected=target;loading=true;mode.SelectedIndex=target;loading=false;LoadEditor();UpdateCaption();await Apply();
            });
            startupItem.Checked=!demo && StartupService.Enabled;startupItem.Enabled=!demo;
            tray.MouseUp+=(s,e)=>{if(e.Button==MouseButtons.Left || e.Button==MouseButtons.Right){if(trayPopup.Visible)trayPopup.Hide();else trayPopup.ShowNear(Cursor.Position);}};
            tray.DoubleClick+=(s,e)=>OpenWindow();
            FormClosed+=(s,e)=>{tray.Visible=false;tray.Dispose();trayPopup.Dispose();};
        }
        void OpenWindow(){if(trayPopup!=null)trayPopup.Hide();ShowInTaskbar=true;Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;BringToFront();Activate();}
        void HideToTray(){Hide();ShowInTaskbar=false;}
        void FitContent(Panel viewport,TableLayoutPanel content){
            // Keep the direct scroll child narrower than the viewport, including at high DPI.
            int width=Math.Max(100,viewport.ClientRectangle.Width-SystemInformation.VerticalScrollBarWidth-2);
            if(content.Width!=width)content.Width=width;
            viewport.HorizontalScroll.Enabled=false;
            viewport.HorizontalScroll.Visible=false;
        }
        void UpdateCaption(){profileCaption.Text="Düzenlenen profil: "+new[]{"Yüksek performans","Oyun","Ofis"}[selected]+". Fan ve RGB ayarları bu moda kaydedilir.";}
        void Add(TableLayoutPanel parent,Control child,int height){int row=parent.RowCount++;parent.Height+=height+22;parent.RowStyles.Add(new RowStyle(SizeType.Absolute,height+22));child.Dock=DockStyle.Fill;child.Margin=new Padding(0,0,0,22);parent.Controls.Add(child,0,row);}
        TableLayoutPanel Grid(Control parent,int rows){var p=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=rows,BackColor=Mac.Card,Margin=Padding.Empty};p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));parent.Controls.Add(p);return p;}
        FlowLayoutPanel Row(){return new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,AutoSize=false,BackColor=Mac.Card,Margin=Padding.Empty,Padding=Padding.Empty};}
        MacButton Button(string text,Action action){var b=new MacButton{Text=text};b.Click+=(s,e)=>{try{action();}catch(Exception ex){feedback.Text=ex.Message;}};return b;}
        MacButton Button(string text,Func<Task> action){var b=new MacButton{Text=text};b.Click+=async(s,e)=>{try{await action();}catch(Exception ex){feedback.Text=ex.Message;}};return b;}
        TableLayoutPanel TitleRow(string text,int actionWidth=190){var p=new TableLayoutPanel{ColumnCount=2,RowCount=1,AutoSize=false,Dock=DockStyle.Fill,BackColor=Mac.Card,Margin=Padding.Empty,Padding=Padding.Empty};p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,actionWidth));p.RowStyles.Add(new RowStyle(SizeType.Percent,100));p.Controls.Add(Mac.Label(text,11,true),0,0);return p;}
        async Task QuickFan(int choice){fan.SelectedIndex=choice;await Apply();}
        void BuildControls(){
            var grid=Grid(controlCard,8);int[] heights={39,28,32,12,40,40,35,39};foreach(int h in heights)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            var performance=TitleRow("Performans modu",310);var buttons=Row();string[] names={"Ofis","Oyun","Yüksek performans"};for(int i=0;i<3;i++){int target=2-i;modeButtons[i]=Button(names[i],async()=>{mode.SelectedIndex=target;await Apply();});modeButtons[i].Width=i==2?150:70;buttons.Controls.Add(modeButtons[i]);}performance.Controls.Add(buttons,1,0);grid.Controls.Add(performance,0,0);
            profileCaption.ForeColor=Mac.Secondary;grid.Controls.Add(profileCaption,0,1);powerLabel.ForeColor=Mac.Secondary;grid.Controls.Add(powerLabel,0,2);
            var divider=new Panel{Dock=DockStyle.Top,Height=1,BackColor=Mac.Line,Margin=new Padding(0,5,0,0)};grid.Controls.Add(divider,0,3);
            var fanRow=TitleRow("Fan kontrolü",380);var fanButtons=Row();fanStatus.Width=135;fanStatus.Dock=DockStyle.None;fanStatus.Height=31;fanStatus.ForeColor=Mac.Secondary;fanButtons.Controls.Add(fanStatus);fanButtons.Controls.Add(Button("Otomatik",async()=>await QuickFan(0)));fanButtons.Controls.Add(Button("Tam hız",async()=>await QuickFan(1)));fanRow.Controls.Add(fanButtons,1,0);grid.Controls.Add(fanRow,0,4);
            var quiet=TitleRow("Kayıtlı fan eğrisi",240);var quietButtons=Row();quietButtons.Controls.Add(Button("Eğriyi aç",async()=>await QuickFan(2)));quietButtons.Controls.Add(Button("Kapat",async()=>await QuickFan(0)));quiet.Controls.Add(quietButtons,1,0);grid.Controls.Add(quiet,0,5);
            var note=Mac.Label("Pencereyi kapatınca tepside çalışır. Çıkış menüsü fanları BIOS yönetimine döndürür.",9);note.ForeColor=Mac.Secondary;grid.Controls.Add(note,0,6);feedback.Font=new Font("Segoe UI",10);feedback.ForeColor=Mac.Secondary;grid.Controls.Add(feedback,0,7);
        }
        void BuildCurve(MacCard card){
            var grid=Grid(card,12);grid.RowStyles.Add(new RowStyle(SizeType.Absolute,38));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,34));grid.RowStyles.Add(new RowStyle(SizeType.Absolute,145));
            var title=TitleRow("Fan eğrisi");title.Controls.Add(Button("Varsayılan",()=>SetCurve(new Profile().Curve)),1,0);grid.Controls.Add(title,0,0);var hint=Mac.Label("CPU/GPU’dan sıcak olanı iki fanı birlikte yönetir. Eğri düzenlenen moda kaydedilir.",9);hint.ForeColor=Mac.Secondary;grid.Controls.Add(hint,0,1);graph.Dock=DockStyle.Fill;grid.Controls.Add(graph,0,2);
            for(int i=0;i<8;i++){grid.RowStyles.Add(new RowStyle(SizeType.Absolute,33));var row=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,BackColor=Mac.Card,Margin=Padding.Empty};row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,85));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,30));row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,62));
                row.Controls.Add(Mac.Label((i+1).ToString(),9),0,0);temperatures[i]=Number(20,79,45);duties[i]=Number(35,100,35);row.Controls.Add(temperatures[i],1,0);row.Controls.Add(Mac.Label("°C",9),2,0);
                var slider=new CleanTrackBar{Minimum=35,Maximum=100,Value=35,TickStyle=TickStyle.None,AutoSize=false,Height=27,Dock=DockStyle.Fill,BackColor=Mac.Card,Margin=Padding.Empty};var percent=Mac.Label("%35",10);int index=i;slider.ValueChanged+=(s,e)=>duties[index].Value=slider.Value;duties[i].ValueChanged+=(s,e)=>{slider.Value=(int)duties[index].Value;percent.Text="%"+duties[index].Value;DrawCurve();};temperatures[i].ValueChanged+=(s,e)=>DrawCurve();row.Controls.Add(slider,3,0);row.Controls.Add(percent,4,0);grid.Controls.Add(row,0,i+3);
            }
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute,43));var bottom=TitleRow("80°C sınırında kontrol BIOS’a döner.");bottom.Controls[0].Font=new Font("Segoe UI",10);bottom.Controls[0].ForeColor=Mac.Secondary;var apply=Button("Kaydet ve uygula",async()=>{fan.SelectedIndex=2;await Apply();});apply.Primary=true;apply.Width=150;bottom.Controls.Add(apply,1,0);grid.Controls.Add(bottom,0,11);
        }
        NumericUpDown Number(int min,int max,int value){return new CleanNumericUpDown{Minimum=min,Maximum=max,Value=value,Width=75,BackColor=Mac.Card,ForeColor=Mac.Text,BorderStyle=BorderStyle.FixedSingle};}
        void BuildRgb(MacCard card){
            var grid=Grid(card,8);int[] sizes={38,32,55,42,40,36,42,38};foreach(int h in sizes)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            grid.Controls.Add(Mac.Label("Klavye RGB",11,true),0,0);
            zones.Text="Bölgeleri ayrı renklendir";zones.Dock=DockStyle.Fill;zones.ForeColor=Mac.Text;grid.Controls.Add(zones,0,1);
            var editor=new Panel{Dock=DockStyle.Fill,BackColor=Mac.Card};
            var general=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,BackColor=Mac.Card};general.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,190));general.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            colorButtons[0]=Button("Renk seç",()=>PickColor(0));colorButtons[0].Dock=DockStyle.Fill;colorButtons[0].Margin=new Padding(0,8,12,8);general.Controls.Add(colorButtons[0],0,0);
            effect.DropDownStyle=ComboBoxStyle.DropDownList;effect.Dock=DockStyle.Fill;effect.Margin=new Padding(0,12,0,0);effect.BackColor=Mac.Card;effect.ForeColor=Mac.Text;effect.FlatStyle=FlatStyle.Flat;
            effect.Items.AddRange(new object[]{"Sabit","Nefes alma","Renk döngüsü","Ambilight","Gökkuşağı geçişi · uygulama","Pastel geçiş · uygulama","Polis ışığı · uygulama","Mum · uygulama","Nabız · uygulama","Sıcaklığa göre · uygulama"});general.Controls.Add(effect,1,0);
            var regions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,BackColor=Mac.Card};for(int i=1;i<4;i++){int index=i;regions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));colorButtons[i]=Button("Bölge "+i,()=>PickColor(index));colorButtons[i].Dock=DockStyle.Fill;colorButtons[i].Margin=new Padding(0,8,12,8);regions.Controls.Add(colorButtons[i],i-1,0);}
            editor.Controls.Add(general);editor.Controls.Add(regions);grid.Controls.Add(editor,0,2);
            var level=TitleRow("Parlaklık",360);var levels=Row();brightness.Minimum=0;brightness.Maximum=2;string[] names={"Kapalı","Düşük","Yüksek"};var brightnessButtons=new MacButton[3];
            for(int i=0;i<3;i++){int value=i;brightnessButtons[i]=Button(names[i],()=>brightness.Value=value);levels.Controls.Add(brightnessButtons[i]);}
            Action syncBrightness=()=>{for(int i=0;i<3;i++)brightnessButtons[i].Selected=i==(int)brightness.Value;};brightness.ValueChanged+=(s,e)=>syncBrightness();syncBrightness();level.Controls.Add(levels,1,0);grid.Controls.Add(level,0,3);
            var timing=Row();var timingLabel=Mac.Label("Geçiş aralığı",10);timingLabel.Dock=DockStyle.None;timingLabel.Width=120;timingLabel.Height=28;timing.Controls.Add(timingLabel);interval.Minimum=4;interval.Maximum=12;interval.Width=65;interval.BackColor=Mac.Card;interval.ForeColor=Mac.Text;timing.Controls.Add(interval);timing.Controls.Add(new Label{Text="sn",ForeColor=Mac.Secondary,AutoSize=true});grid.Controls.Add(timing,0,4);
            rgbStatus.ForeColor=Mac.Secondary;grid.Controls.Add(rgbStatus,0,5);
            var buttons=Row();rgbApply=Button("Uygula",async()=>await ApplyRgb());rgbApply.Primary=true;buttons.Controls.Add(rgbApply);rgbStop=Button("Efekti durdur",async()=>await Execute(async()=>{await client.Send(new Command{Action="rgbstop"});rgbStatus.Text="Efekt durdu; son gönderilen renk klavyede kalır.";}));rgbStop.Width=145;rgbStop.Enabled=false;buttons.Controls.Add(rgbStop);grid.Controls.Add(buttons,0,6);
            var note=Mac.Label("Renk kutuları seçilen/kayıtlı rengi gösterir. Uygula, seçimi kalıcı olarak kaydeder.",9);note.ForeColor=Mac.Secondary;grid.Controls.Add(note,0,7);
            Action update=()=>{general.Visible=!zones.Checked;regions.Visible=zones.Checked;bool software=!zones.Checked && effect.SelectedIndex>=4;timing.Visible=software;grid.RowStyles[4].Height=software?40:0;};zones.CheckedChanged+=(s,e)=>update();effect.SelectedIndexChanged+=(s,e)=>update();update();
        }
        Profile RgbEditor(){var p=new Profile{RgbEnabled=true,ZonesEnabled=zones.Checked,Effect=Effects.Names[effect.SelectedIndex],Brightness=(int)brightness.Value,Interval=(int)interval.Value,Color=colorButtons[0].BackColor.ToArgb()&0xffffff,Zones=new[]{colorButtons[1].BackColor.ToArgb()&0xffffff,colorButtons[2].BackColor.ToArgb()&0xffffff,colorButtons[3].BackColor.ToArgb()&0xffffff}};p.Validate();return p;}
        void LoadRgb(Profile p){
            rgb.Checked=p.RgbEnabled;zones.Checked=p.ZonesEnabled;effect.SelectedIndex=Array.IndexOf(Effects.Names,p.Effect);brightness.Value=p.Brightness;interval.Value=p.Interval;
            SetColor(0,p.Color);for(int i=0;i<3;i++)SetColor(i+1,p.Zones[i]);
            rgbStatus.Text=p.RgbEnabled?(p.ZonesEnabled?"Kayıtlı bölge renkleri yüklendi.":"Kayıtlı renk: #"+p.Color.ToString("X6")):"Bir renk seçip Uygula'ya basın.";
        }
        async Task ApplyRgb(){
            var p=RgbEditor();int target=selected;
            await Execute(async()=>{
                await client.Send(new Command{Action="rgb",Mode=target,Profile=p});
                settings.Profiles[target].CopyRgbFrom(p);settings.LastRgb=p.RgbCopy();settings.LastMode=target;rgb.Checked=true;
                ProfileStore.Write(ProfileStore.FilePath,settings);startupDone=true;
                rgbStatus.Text=p.ZonesEnabled?"Bölge renkleri uygulandı ve kaydedildi.":"Uygulanan renk: #"+p.Color.ToString("X6");feedback.Text="RGB uygulandı ve kaydedildi; fan ayarları değişmedi.";
            });
        }
        void PickColor(int index){using(var d=new ColorDialog{Color=colorButtons[index].BackColor,FullOpen=true})if(d.ShowDialog()==DialogResult.OK)SetColor(index,d.Color.ToArgb()&0xffffff);}
        void SetColor(int i,int color){((MacButton)colorButtons[i]).UseSwatch=true;colorButtons[i].BackColor=Color.FromArgb(255,(color>>16)&255,(color>>8)&255,color&255);colorButtons[i].ForeColor=colorButtons[i].BackColor.GetBrightness()>0.55?Color.Black:Color.White;colorButtons[i].Text=(i==0?"Renk":new[]{"","Sol","Orta","Sağ"}[i])+" · #"+color.ToString("X6");colorButtons[i].Invalidate();}
        PointSetting[] ReadCurve(){var p=new PointSetting[8];for(int i=0;i<8;i++)p[i]=new PointSetting{Temperature=(int)temperatures[i].Value,Duty=(int)duties[i].Value};return p;}
        void DrawCurve(){if(temperatures[7]==null || duties[7]==null)return;graph.Points=ReadCurve();graph.Invalidate();}
        void SetCurve(PointSetting[] curve){for(int i=0;i<8;i++){temperatures[i].Value=curve[i].Temperature;duties[i].Value=curve[i].Duty;}DrawCurve();}
        void LoadEditor(){LoadProfile(settings.Profiles[selected]);}
        void LoadProfile(Profile p){loading=true;fan.SelectedIndex=Array.IndexOf(new[]{"auto","max","curve"},p.Fan);SetCurve(p.Curve);LoadRgb(p);loading=false;}
        Profile Editor(){var p=new Profile{Fan=new[]{"auto","max","curve"}[fan.SelectedIndex],Curve=ReadCurve(),RgbEnabled=rgb.Checked,ZonesEnabled=zones.Checked,Effect=Effects.Names[effect.SelectedIndex],Brightness=(int)brightness.Value,Interval=(int)interval.Value,Color=colorButtons[0].BackColor.ToArgb()&0xffffff,Zones=new[]{colorButtons[1].BackColor.ToArgb()&0xffffff,colorButtons[2].BackColor.ToArgb()&0xffffff,colorButtons[3].BackColor.ToArgb()&0xffffff}};p.Validate();return p;}
        void SaveEditor(){settings.Profiles[selected]=Editor();settings.LastMode=selected;settings.Remember=remember.Checked;}
        async Task Apply(){
            var p=Editor();int target=selected;
            await Execute(async()=>{await client.Send(new Command{Action="apply",Mode=target,Profile=p});settings.Profiles[target]=p;if(p.RgbEnabled)settings.LastRgb=p.RgbCopy();settings.LastMode=target;settings.Remember=remember.Checked;ProfileStore.Write(ProfileStore.FilePath,settings);startupDone=true;feedback.Text="Profil uygulandı ve kaydedildi.";});
        }
        async Task Execute(Func<Task> action){
            if(busy || closing || client==null || !connected)return;
            busy=true;controlCard.Enabled=false;muxActions.Enabled=false;mode.Enabled=false;
            try{await action();}catch(Exception ex){feedback.Text="İşlem başarısız: "+ex.Message;}finally{busy=false;controlCard.Enabled=true;muxActions.Enabled=connected;mode.Enabled=true;}
        }
        async Task SyncWindowsPowerPlan(Snapshot snapshot){
            if(demo){powerLabel.Text="Windows güç planı: demo modunda değiştirilmez.";return;}
            try{var plan=await Task.Run(()=>WindowsPowerPlans.Active());powerLabel.Text="Windows güç planı: "+plan.Name;}
            catch(Exception ex){powerLabel.Text="Güç planı okunamadı: "+ex.Message;}
        }
        async Task RefreshStatus(){
            if(busy || closing || client==null)return;busy=true;
            try{
                var r=await client.Send(new Command{Action="read"});connected=!r.RestorePending;
                rgbStop.Enabled=r.RgbSoftwareEffect;
                cpu.Set(r.Status.Cpu,r.Status.CpuRpm);if(r.Status.GpuAvailable)gpu.Set(r.Status.Gpu,r.Status.GpuRpm);else gpu.Clear();
                history.Add(r.Status.Cpu,r.Status.Gpu);
                if(trayPopup!=null)trayPopup.UpdateStatus(r.Status,true,r.Fan,r.Duty);
                link.Text=demo?"●  Canlı bağlantı · Demo":"●  Canlı bağlantı";link.ForeColor=Color.FromArgb(47,170,90);
                fanStatus.Text=r.Fan=="curve"?"Sessiz · %"+r.Duty+" istek":r.Fan=="max"?"Tam hız isteği etkin":"BIOS yönetimi";
                footer.Text="BIOS modu: "+new[]{"Yüksek performans","Oyun","Ofis"}[r.Status.Mode]+"                         Kontrol sürümü · 0.6.3";
                for(int i=0;i<3;i++)modeButtons[i].Selected=(2-i)==r.Status.Mode;
                if(!muxInitialized){muxInitialized=true;try{var muxReply=await client.Send(new Command{Action="muxread"});ShowMux(muxReply.Mux);}catch(Exception muxError){muxLabel.Text="MUX durumu okunamadı: "+muxError.Message;}}
                if(!startupDone && DateTime.UtcNow>=nextRestoreAttempt){
                    nextRestoreAttempt=DateTime.UtcNow.AddSeconds(10);
                    if(settings.Remember){r=await client.Send(new Command{Action="apply",Mode=settings.LastMode,Profile=settings.Profiles[settings.LastMode]});startupDone=true;feedback.Text="Son kaydedilen profil ve ayarlar yüklendi.";}
                    else{startupDone=true;feedback.Text=demo?"DEMO: örnek veriler; donanıma erişilmez.":"Bağlantı kuruldu. Açılışta son ayarları uygulama kapalı.";}
                }
                if(startupDone && connected)await SyncWindowsPowerPlan(r.Status);
                if(r.Message!="Hazır." && r.Message!="Profil uygulandı.")feedback.Text=r.Message;
            }catch(Exception ex){connected=false;if(trayPopup!=null)trayPopup.UpdateStatus(null,false,"auto",255);cpu.Clear();gpu.Clear();link.Text="●  Veri akışı bekleniyor";link.ForeColor=Color.DarkOrange;feedback.Text=ex.Message;}
            finally{busy=false;muxActions.Enabled=connected;}
        }
        async void OnClosing(object sender,FormClosingEventArgs e){
            if(!exitRequested && e.CloseReason==CloseReason.UserClosing){e.Cancel=true;HideToTray();return;}
            if(e.CloseReason==CloseReason.WindowsShutDown || e.CloseReason==CloseReason.TaskManagerClosing){timer.Stop();if(client!=null)client.Dispose();return;}
            if(closing){e.Cancel=true;return;}if(client==null)return;
            if(client.Broken){timer.Stop();client.Dispose();return;}
            if(busy){e.Cancel=true;feedback.Text="İşlem sürüyor; tamamlandıktan sonra kapatın.";return;}
            e.Cancel=true;closing=true;timer.Stop();
            try{await client.Send(new Command{Action="stop"});client.Dispose();FormClosing-=OnClosing;Close();}
            catch(Exception ex){closing=false;feedback.Text="BIOS'a dönüş doğrulanamadı: "+ex.Message+" Uygulamayı kapatmak gözetim sürecini de sonlandırmaz.";timer.Start();}
        }
    }
    sealed class TrayPopup : Form {
        readonly Label cpuValue=Mac.Label("—",24,true),gpuValue=Mac.Label("—",24,true);
        readonly Label cpuFan=Mac.Label("— RPM",9),gpuFan=Mac.Label("— RPM",9),status=Mac.Label("Bağlantı bekleniyor",9),fanState=Mac.Label("BIOS yönetimi",9);
        readonly MacButton[] modes=new MacButton[3];
        readonly System.Drawing.Bitmap logo;
        readonly System.Windows.Forms.Timer revealTimer=new System.Windows.Forms.Timer{Interval=15};
        readonly System.Diagnostics.Stopwatch revealClock=new System.Diagnostics.Stopwatch();
        Point revealTarget;
        public TrayPopup(Icon icon,Action open,Action exit,Action<int> applyMode){
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(360,355);BackColor=Mac.Background;Font=new Font("Segoe UI",10);Padding=new Padding(18);KeyPreview=true;
            var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,BackColor=Mac.Background};int[] heights={54,112,42,35,45,25};foreach(int h in heights)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
            var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};header.RowStyles.Add(new RowStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,44));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            if(icon!=null){logo=icon.ToBitmap();header.Controls.Add(new PictureBox{Image=logo,SizeMode=PictureBoxSizeMode.Zoom,Size=new Size(34,34),Anchor=AnchorStyles.Left,Margin=Padding.Empty},0,0);}
            var heading=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};heading.RowStyles.Add(new RowStyle(SizeType.Absolute,27));heading.Controls.Add(Mac.Label("G870 Kontrol Merkezi",12,true),0,0);status.ForeColor=Mac.Secondary;heading.Controls.Add(status,0,1);header.Controls.Add(heading,1,0);grid.Controls.Add(header,0,0);
            var sensors=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};sensors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));sensors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));sensors.Controls.Add(Sensor("CPU",cpuValue,cpuFan,Mac.Cyan),0,0);sensors.Controls.Add(Sensor("GPU",gpuValue,gpuFan,Mac.Orange),1,0);grid.Controls.Add(sensors,0,1);
            var switches=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3};string[] names={"Ofis","Oyun","Performans"};for(int i=0;i<3;i++){int target=2-i;switches.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));modes[i]=new MacButton{Text=names[i],HideFocusRing=true,Dock=DockStyle.Fill,Margin=new Padding(2,4,2,5),Enabled=false};modes[i].Click+=(s,e)=>applyMode(target);switches.Controls.Add(modes[i],i,0);}grid.Controls.Add(switches,0,2);
            fanState.ForeColor=Mac.Secondary;grid.Controls.Add(fanState,0,3);
            var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,70));actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));var openButton=new MacButton{Text="Uygulamayı aç",Primary=true,Dock=DockStyle.Fill,Margin=new Padding(0,2,7,6)};openButton.Click+=(s,e)=>open();var exitButton=new MacButton{Text="Çıkış",Dock=DockStyle.Fill,Margin=new Padding(0,2,0,6)};exitButton.Click+=(s,e)=>{Hide();exit();};actions.Controls.Add(openButton,0,0);actions.Controls.Add(exitButton,1,0);grid.Controls.Add(actions,0,4);
            var hint=Mac.Label("Pencere kapalıyken denetim devam eder.",8);hint.ForeColor=Mac.Secondary;grid.Controls.Add(hint,0,5);Controls.Add(grid);
            revealTimer.Tick+=(s,e)=>{double t=Math.Min(1,revealClock.Elapsed.TotalMilliseconds/170.0);double eased=1-Math.Pow(1-t,3);Opacity=eased;Location=new Point(revealTarget.X,revealTarget.Y+(int)Math.Round(10*(1-eased)));if(t>=1){revealTimer.Stop();Opacity=1;Location=revealTarget;}};
            VisibleChanged+=(s,e)=>{if(!Visible){revealTimer.Stop();revealClock.Reset();}};
            Deactivate+=(s,e)=>Hide();KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)Hide();};FormClosed+=(s,e)=>{revealTimer.Dispose();if(logo!=null)logo.Dispose();};
        }
        Panel Sensor(string title,Label value,Label fan,Color accent){var card=new MacCard{Dock=DockStyle.Fill,Padding=new Padding(14,8,10,8),Margin=new Padding(2,5,4,8)};var label=Mac.Label(title,9,true);label.ForeColor=accent;label.BackColor=value.BackColor=fan.BackColor=Mac.Card;label.Dock=DockStyle.Top;label.Height=22;value.Dock=DockStyle.Fill;fan.Dock=DockStyle.Bottom;fan.Height=21;fan.ForeColor=Mac.Secondary;card.Controls.Add(value);card.Controls.Add(label);card.Controls.Add(fan);return card;}
        public void UpdateStatus(Snapshot snapshot,bool online,string fan,int duty){
            status.Text=online?"● Canlı bağlantı":"● Bağlantı yok";status.ForeColor=online?Color.FromArgb(47,170,90):Mac.Secondary;
            cpuValue.Text=online?snapshot.Cpu+"°C":"—";gpuValue.Text=online && snapshot.GpuAvailable?snapshot.Gpu+"°C":"—";
            cpuFan.Text=online?snapshot.CpuRpm+" RPM":"— RPM";gpuFan.Text=online && snapshot.GpuAvailable?snapshot.GpuRpm+" RPM":"— RPM";
            for(int i=0;i<3;i++){modes[i].Enabled=online;modes[i].Selected=online && snapshot.Mode==2-i;}
            fanState.Text=fan=="curve"?"Fan eğrisi etkin · %"+duty:fan=="max"?"Fanlar tam hızda":"Fanlar BIOS tarafından yönetiliyor";
        }
        public void ShowNear(Point anchor){revealTimer.Stop();var area=Screen.FromPoint(anchor).WorkingArea;revealTarget=new Point(Math.Max(area.Left+10,Math.Min(anchor.X-Width+20,area.Right-Width-10)),Math.Max(area.Top+10,Math.Min(anchor.Y-Height-12,area.Bottom-Height-20)));Opacity=0;Location=new Point(revealTarget.X,revealTarget.Y+10);Show();Activate();revealClock.Restart();revealTimer.Start();}
    }
    static class Program {
        [System.Runtime.InteropServices.DllImport("user32.dll",SetLastError=true)]
        static extern bool AllowSetForegroundWindow(uint processId);
        static void PermitExistingWindow(){
            using(var current=System.Diagnostics.Process.GetCurrentProcess()){
                foreach(var process in System.Diagnostics.Process.GetProcessesByName(current.ProcessName)){
                    using(process)try{if(process.Id!=current.Id && process.SessionId==current.SessionId)AllowSetForegroundWindow((uint)process.Id);}catch{}
                }
            }
        }
        [STAThread] static void Main(string[] args){
            bool demo=Array.IndexOf(args,"--demo")>=0;
            if(args.Length>=3 && args[0]=="--worker"){Worker.Run(int.Parse(args[1]),long.Parse(args[2]),demo);return;}
            using(var activation=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\G870WindowsControlCenter.Activate"))
            using(var mutex=new Mutex(false,@"Local\G870WindowsControlCenter")){
                bool ownsMutex;
                try{ownsMutex=mutex.WaitOne(0);}catch(AbandonedMutexException){ownsMutex=true;}
                if(!ownsMutex){PermitExistingWindow();activation.Set();return;}
                try{Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainWindow(demo,Array.IndexOf(args,"--tray")>=0,activation));}
                finally{mutex.ReleaseMutex();}
            }
        }
    }
}
