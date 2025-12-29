using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace TimeSyncService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller serviceProcessInstaller;

        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.serviceInstaller = new ServiceInstaller();
            this.serviceProcessInstaller = new ServiceProcessInstaller();

            // 
            // serviceInstaller
            // 
            this.serviceInstaller.Description = "Synchronizes time across biometric attendance devices";
            this.serviceInstaller.DisplayName = "Biometric Time Sync Service";
            this.serviceInstaller.ServiceName = "TimeSyncService";
            this.serviceInstaller.StartType = ServiceStartMode.Manual;

            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;

            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new Installer[] {
                this.serviceProcessInstaller,
                this.serviceInstaller
            });
        }
    }
}
