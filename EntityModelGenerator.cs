using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows.Forms;

namespace EntityModelGenerator
{
    public partial class EntityModelGenerator : Form
    {
        SqlConnection conn;

        public EntityModelGenerator()
        {
            InitializeComponent();
        }

        private void EntityModelGenerator_Shown(object sender, EventArgs e)
        {
            using (frmLogin login = new frmLogin())
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    if (conn == null)
                    {
                        conn = new SqlConnection(Database.ConnectionString("master"));
                        conn.Open();

                        SqlDataAdapter adapter = new SqlDataAdapter("select [name] from sys.sysdatabases where [name] not in ('model','master','msdb','tempdb')", conn);

                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        foreach (DataRow row in dt.Rows)
                        {
                            cboDatabases.Items.Add(row["name"]);
                        }
                    }
                }
                else
                {
                    this.Close();
                }
            }

        }

        private void EntityModelGenerator_Load(object sender, EventArgs e)
        {
           
        }
        
        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Project Files (*.proj, *.csproj, *.xproj) | *.proj; *.csproj; *.xproj";

                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    textFileName.Text = dialog.FileName;
                }
            }
        }

        private void btnGetDBObjectList_Click(object sender, EventArgs e)
        {
            this.dataSetList.Clear();

            if (string.IsNullOrWhiteSpace(cboDatabases.Text))
            {
                MessageBox.Show("Please select database.", "Required Database", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (!chkTable.Checked && !chkView.Checked)
            {
                MessageBox.Show("Please select table and/or view to load.", "Required Object", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            try
            {
                if (conn != null)
                {
                    if (conn.State == System.Data.ConnectionState.Open ||
                        conn.State == System.Data.ConnectionState.Fetching ||
                        conn.State == System.Data.ConnectionState.Executing)
                    {
                        conn.Close();
                        conn = null;
                    }
                }

                conn = new SqlConnection(Database.ConnectionString(cboDatabases.Text));

                conn.Open();

                System.Text.StringBuilder query = new System.Text.StringBuilder();
                query.AppendLine("select a.name, a.type_desc as type, b.[name] as [schema], cast(0 as bit) isselect from sys.objects a ");
                query.AppendLine("inner join sys.schemas b on a.schema_id = b.schema_id ");
                query.Append("where a.[type] in (");
                if (chkTable.Checked)
                {
                    query.Append("'U',");
                }
                if (chkView.Checked)
                {
                    query.Append("'V')");
                }
                if (query.ToString().EndsWith(","))
                {
                    query.Remove(query.Length - 1, 1);
                    query.Append(")");
                }

                query.Append(" order by a.[name]");


                SqlDataAdapter adapter = new SqlDataAdapter(query.ToString(), conn);

                adapter.Fill(this.dataSetList.Tables[0]);

                btnGetDBObjectList.Text = "Reload List";

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Entity Model Generator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (conn != null)
                {
                    if (conn.State == System.Data.ConnectionState.Open ||
                        conn.State == System.Data.ConnectionState.Fetching ||
                        conn.State == System.Data.ConnectionState.Executing)
                    {
                        conn.Close();
                        conn = null;
                    }
                }
            }
           
        }

        private void btnGenerateDataModel_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textFileName.Text))
            {
                MessageBox.Show("No specfied target project.", "Entity Model Generator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (dataSetList.Tables[0].Rows.Count == 0)
            {
                MessageBox.Show("Please load list of table/view first.", "Entity Model Generator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            DataRow[] rows = dataSetList.Tables[0].Select("IsSelect = 1");
            if (rows.Length == 0)
            {
                MessageBox.Show("No data selected.", "Entity Model Generator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Cursor = Cursors.WaitCursor;
            //check repository folder
            DirectoryInfo info = new DirectoryInfo(textFileName.Text);
            string repositoryFolder = string.Format(@"{0}\Repository", info.Parent.FullName);
            if (!Directory.Exists(repositoryFolder))
            {
                Directory.CreateDirectory(repositoryFolder);
            }

            //check datamode folder
            string dataModelFolder = string.Format(@"{0}\DataModel", repositoryFolder);
            if (!Directory.Exists(dataModelFolder))
            {
                Directory.CreateDirectory(dataModelFolder);
            }

            //check datamode folder
            string viewModelFolder = string.Format(@"{0}\ViewModel", repositoryFolder);
            if (!Directory.Exists(viewModelFolder))
            {
                Directory.CreateDirectory(viewModelFolder);
            }

            //check repository folder
            string serviceFolder = string.Format(@"{0}\Service", info.Parent.FullName);
            if (!Directory.Exists(serviceFolder))
            {
                Directory.CreateDirectory(serviceFolder);
            }

            string projectName = info.Name.Replace(info.Extension, "");
            ClassGenerator file = new ClassGenerator(projectName, repositoryFolder, dataModelFolder, viewModelFolder, serviceFolder);
            
            file.CreateRepository();
            file.CreateService();
            file.CreateDabaseContext(cboDatabases.Text, rows);

            foreach (DataRow row in rows)
            {
                file.CreateEntity(row["name"].ToString(), row["schema"].ToString(), cboDatabases.Text);
            }

            Cursor = Cursors.Default;

            MessageBox.Show("Generate model completed.", "Entity Model Generator", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
                
    }
}
