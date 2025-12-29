using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DataCollectionService
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
            this.serviceInstaller.Description = "Collects attendance data from biometric devices";
            this.serviceInstaller.DisplayName = "Biometric Data Collection Service";
            this.serviceInstaller.ServiceName = "DataCollectionService";
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
