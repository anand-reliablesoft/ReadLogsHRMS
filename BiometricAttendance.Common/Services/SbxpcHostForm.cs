using System;
using System.Windows.Forms;

namespace BiometricAttendance.Common.Services
{
    /// <summary>
    /// AxHost wrapper for SBXPC ActiveX control
    /// </summary>
    internal class SbxpcAxHost : AxHost
    {
        public SbxpcAxHost(string clsid) : base(clsid)
        {
        }
        
        public dynamic GetOcxObject()
        {
            return this.GetOcx();
        }
        
        // Expose methods to call on the control
        public bool CallSetIPAddress(string ipAddress, int port, int password)
        {
            try
            {
                // Use InvokeMethod to call the SetIPAddress method on the ActiveX control
                object result = this.GetType().InvokeMember("SetIPAddress",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    this,
                    new object[] { ipAddress, port, password });
                return Convert.ToBoolean(result);
            }
            catch
            {
                // If that doesn't work, try through the OCX
                dynamic ocx = this.GetOcx();
                return ocx.SetIPAddress(ipAddress, port, password);
            }
        }
    }
    
    /// <summary>
    /// Hidden form to host the SBXPC ActiveX control
    /// Required because SBXPC is a visual control that needs a window handle
    /// </summary>
    internal class SbxpcHostForm : Form
    {
        private SbxpcAxHost _axHost;
        
        public SbxpcHostForm()
        {
            // Create hidden form
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);
            this.Opacity = 0;
            
            // Don't show the form
            this.Load += (s, e) => this.Hide();
        }
        
        /// <summary>
        /// Creates and hosts the SBXPC ActiveX control
        /// </summary>
        public dynamic CreateSbxpcControl()
        {
            try
            {
                // Get the CLSID for SBXPC
                Type comType = Type.GetTypeFromProgID("SBXPC.SBXPCCtrl.1");
                if (comType == null)
                {
                    throw new InvalidOperationException("SBXPC ActiveX control is not registered.");
                }
                
                Guid clsid = comType.GUID;
                
                // Create AxHost to host the ActiveX control
                _axHost = new SbxpcAxHost(clsid.ToString());
                _axHost.BeginInit();
                
                // Add to form's controls
                this.Controls.Add(_axHost);
                
                _axHost.EndInit();
                
                // Return the AxHost itself - it will forward calls to the OCX
                return _axHost;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create SBXPC control in host form.", ex);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_axHost != null)
                {
                    _axHost.Dispose();
                    _axHost = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
