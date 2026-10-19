using System;
using System.IO;
using System.Management;
namespace G870Windows {
    sealed class MuxState {
        public uint Current, Boot, ExternalDisplay;
        public uint Requested;
        public bool Supported;
        public string Message;
    }
    static class MuxSafety {
        public static void CheckEncryption(){
            string drive=Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)).TrimEnd('\\');
            var scope=new ManagementScope(@"\\.\root\CIMV2\Security\MicrosoftVolumeEncryption");
            scope.Options.EnablePrivileges=true;scope.Options.Timeout=TimeSpan.FromSeconds(5);
            try{
                using(var search=new ManagementObjectSearcher(scope,new ObjectQuery("SELECT DriveLetter,ProtectionStatus FROM Win32_EncryptableVolume WHERE DriveLetter='"+drive+"'"),new EnumerationOptions{Timeout=TimeSpan.FromSeconds(5)}))
                using(var items=search.Get()){
                    bool found=false;
                    foreach(ManagementObject volume in items)using(volume){
                        found=true;if(volume["ProtectionStatus"]==null || Convert.ToUInt32(volume["ProtectionStatus"])!=0)
                            throw new IOException("BitLocker koruması etkin veya durumu belirsiz. MUX değişikliği engellendi. Önce Windows'ta kurtarma anahtarını ve BitLocker durumunu kontrol edin.");
                    }
                    if(!found)throw new IOException("Sistem diskinin BitLocker durumu doğrulanamadı; MUX değişikliği gönderilmedi.");
                }
            }catch(ManagementException ex){throw new IOException("BitLocker durumu okunamadı; MUX değişikliği gönderilmedi: "+ex.Message,ex);}
        }
    }
}
