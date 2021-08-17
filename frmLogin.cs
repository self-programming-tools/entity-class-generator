using System;
using System.Configuration;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Text;

namespace EntityModelGenerator
{
    public partial class frmLogin : Form
    {
        Configuration config;
        public frmLogin()
        {
            InitializeComponent();
            this.cboAuthentication.SelectedIndex = 0;
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            var server = config.AppSettings.Settings["servername"];
            if (server != null)
            {
                txtServerName.Text = server.Value;
            }

            var auth = config.AppSettings.Settings["authentication"];
            if (auth == null)
            {
                cboAuthentication.SelectedIndex = 0;
            }
            else
            {
                switch (auth.Value)
                {
                    case "Windows Authentication":
                        txtLogin.Enabled = false;
                        txtPassword.Enabled = false;
                        chkRememberPassword.Enabled = false;
                        break;

                    default:
                        txtLogin.Enabled = true;
                        txtPassword.Enabled = true;
                        cboAuthentication.SelectedIndex = 1;
                        break;
                }
            }
        }     

        private void btnConnect_Click(object sender, EventArgs e)
        {
            Database.Password = txtPassword.Text;
            Database.ServerName = txtServerName.Text;
            Database.Login = txtLogin.Text;
            Database.Authentication = cboAuthentication.SelectedIndex;


            SqlConnection conn = new SqlConnection(Database.ConnectionString("master"));

            try
            {
                conn.Open();

                if (conn.State == System.Data.ConnectionState.Open)
                {
                    config.AppSettings.Settings.Remove("authentication");
                    config.AppSettings.Settings.Add("authentication", cboAuthentication.Text);

                    config.AppSettings.Settings.Remove("servername");
                    config.AppSettings.Settings.Add("servername", txtServerName.Text);

                    config.AppSettings.Settings.Remove("isRemember");
                    config.AppSettings.Settings.Remove("uname");
                    config.AppSettings.Settings.Remove("pw");

                    if (chkRememberPassword.Checked)
                    {
                        
                        config.AppSettings.Settings.Add("isRemember", chkRememberPassword.Checked.ToString());
                        config.AppSettings.Settings.Add("uname", txtLogin.Text);
                        config.AppSettings.Settings.Add("pw", Convert.ToBase64String(Encoding.ASCII.GetBytes(txtPassword.Text)));
                    }

                    config.Save();
                    conn.Close();

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    
                }
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                }

            }
            catch (Exception ex)
            {
                this.DialogResult = DialogResult.Cancel;
                MessageBox.Show(ex.Message, "Entity Model Generator");
            }
        }

        private void cboAuthentication_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtLogin.Enabled = cboAuthentication.SelectedIndex == 1;
            txtPassword.Enabled = cboAuthentication.SelectedIndex == 1;
            chkRememberPassword.Enabled = cboAuthentication.SelectedIndex == 1;

            if (cboAuthentication.SelectedIndex == 1)
            {
                var pw = config.AppSettings.Settings["isRemember"];
                if (pw != null)
                {
                    switch (pw.Value)
                    {
                        case "True":
                        case "1":
                            chkRememberPassword.Checked = true;

                            var login = config.AppSettings.Settings["uname"];
                            if (login != null)
                            {
                                txtLogin.Text = login.Value;
                            }
                            

                            var _pw = config.AppSettings.Settings["pw"];
                            if (_pw != null)
                            {
                                txtPassword.Text = Encoding.UTF8.GetString(Convert.FromBase64String(_pw.Value));
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
