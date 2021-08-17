using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace EntityModelGenerator
{
    public class ClassGenerator
    {
        SqlConnection conn;
        StringBuilder build = new StringBuilder();

        private string projectName, repopath, datamodelpath, viewmodelpath, servicepath;

        public ClassGenerator(string _projectName, string _repository, string _datamodel, string _viewmodel, string _service)
        {
            projectName = _projectName;
            repopath = _repository;
            datamodelpath = _datamodel;
            viewmodelpath = _viewmodel;
            servicepath = _service;
        }

        public void CreateEntity(string tableName, string schema, string databaseName)
        {
            string filename = string.Format(@"{0}\{1}.cs", datamodelpath, tableName);
            if (!System.IO.File.Exists(filename))
            {
                build.Clear();

                AppendLine("using System;");
                AppendLine("using System.ComponentModel.DataAnnotations;");
                AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
                build.AppendLine();

                AppendLine(string.Format("namespace {0}.Repository.DataModel", this.projectName));
                AppendLine("{");
                AppendLine(string.Format("[Table(\"{0}\", Schema=\"{1}\")]", tableName, schema), 1);
                AppendLine(string.Format("public class {0}", projectName), 1);
                AppendLine("{", 1);

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

                    conn = new SqlConnection(Database.ConnectionString(databaseName));

                    conn.Open();

                    System.Text.StringBuilder query = new System.Text.StringBuilder();
                    query.AppendLine("select c.column_name, data_type, is_nullable, character_maximum_length, x.constraint_name, x.constraint_type, x.table_catalog ");
                    query.AppendLine("from information_schema.columns c");
                    query.AppendLine("left join (");
		            query.AppendLine("select ccu.constraint_name, tc.constraint_type, tc.table_catalog, ccu.column_name, ccu.table_name ");
		            query.AppendLine("from information_schema.constraint_column_usage as ccu ");
                    query.AppendLine("left join information_schema.table_constraints as tc on ccu.constraint_name = tc.constraint_name ");
		            query.AppendLine("where tc.constraint_type = 'PRIMARY KEY' ");
	                query.AppendLine(") x on c.column_name = x.column_name and c.table_name = x.table_name ");
                    query.AppendFormat("where c.table_name = '{0}' and c.table_schema = '{1}'", tableName, schema);
                    query.AppendLine();
                    query.Append("order by ordinal_position");
                    
                    SqlDataAdapter adapter = new SqlDataAdapter(query.ToString(), conn);

                    System.Data.DataTable table = new System.Data.DataTable(tableName);
                    adapter.Fill(table);

                    foreach (DataRow row in table.Rows)
                    {
                        string datatype = GetDataType(row["data_type"]);
                        if (datatype == "string")
                        {
                            var strLen = string.Format("{0}", row["character_maximum_length"]);
                            if (!string.IsNullOrEmpty(strLen))
                            {
                                int val = 0;
                                int.TryParse(strLen, out val);
                                if (val > 0)
                                {
                                    AppendLine(string.Format("[StringLength({0})]", row["character_maximum_length"]), 2);
                                }
                            }
                            
                        }
                        if (row["is_nullable"].ToString() == "NO")
                        {
                            AppendLine("[Required]", 2);
                        }
                        if (string.Format("{0}", row["constraint_name"]).Length > 0)
                        {
                            AppendLine("[Key]", 2);
                        }

                        Append("public ", 2);
                        Append(datatype + " ");

                        if (row["is_nullable"] == "NO" && datatype != "string")
                        {
                            Append("? ");
                        }

                        Append(row["column_name"].ToString() + " { get; set; }");
                        AppendLine("");
                        AppendLine("");
                    }


                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Entity Model Generator");
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

                AppendLine("}", 1);
                AppendLine("}");

                System.IO.File.WriteAllText(filename, build.ToString());
            }
        }

        private string GetDataType(object data)
        {
            string datatype = "NotSupported_Please_FixIt";
            switch (string.Format("{0}", data))
            {
                case "int":
                case "smallint":
                    datatype = "int";
                    break;

                case "varchar":
                case "nvarchar":
                case "char":
                    datatype = "string";
                    break;

                case "bit":
                    datatype = "bool";
                    break;

                case "date":
                case "datetime":
                case "smalldatetime":
                    datatype = "DateTime";
                    break;

                case "numeric":
                case "decimal":
                    datatype = "decimal";
                    break;

                default:
                    break;
            }
            return datatype;
        }

        public void CreateRepository()
        {
            string filename = string.Format(@"{0}\{1}Repository.cs", repopath, projectName);
            if (!System.IO.File.Exists(filename))
            {
                build.Clear();

                AppendLine("using System;");
                AppendLine("using System.Collections.Generic;");
                AppendLine("using System.Linq;");
                AppendLine(string.Format("using {0}.Repository.DataModel;", projectName));
                build.AppendLine();

                AppendLine(string.Format("namespace {0}.Repository", this.projectName));
                AppendLine("{");
                AppendLine(string.Format("public class {0}Repository : I{0}", projectName), 1);
                AppendLine("{", 1);
                build.AppendLine();

                AppendLine("#region Context", 2);
                AppendLine("private DatabaseContext _context;", 2);
                AppendLine("private DatabaseContext Context", 2);
                AppendLine("{", 2);
                AppendLine("get", 3);
                AppendLine("{", 3);
                AppendLine("if (_context == null)", 4);
                AppendLine("{", 4);
                AppendLine("_context = new DatabaseContext();", 5);
                AppendLine("}", 4);
                AppendLine("return _context", 4);
                AppendLine("}", 3);
                AppendLine("}", 2);
                AppendLine("#endregion", 2);
                AppendLine("}", 1);
                AppendLine("}");

                System.IO.File.WriteAllText(filename, build.ToString());
            }

            CreateRepositoryInterface();
        }

        private void CreateRepositoryInterface()
        {
            string filename = string.Format(@"{0}\I{1}Repository.cs", repopath, projectName);
            if (!System.IO.File.Exists(filename))
            {
                build.Clear();

                AppendLine("using System;");
                AppendLine("using System.Collections.Generic;");
                AppendLine(string.Format("using {0}.Repository.ViewModel;", projectName));
                AppendLine(string.Format("using {0}.Repository.DataModel;", projectName));
                build.AppendLine();

                AppendLine(string.Format("namespace {0}.Repository", this.projectName));
                AppendLine("{");
                AppendLine(string.Format("public interface I{0}Repository : I{0}", projectName), 1);
                AppendLine("{", 1);
                AppendLine("}", 1);
                AppendLine("}");

                System.IO.File.WriteAllText(filename, build.ToString());
            }
        }

        public void CreateService()
        {
            string filename = string.Format(@"{0}\{1}Service.cs", servicepath, projectName);
            if (!System.IO.File.Exists(filename))
            {
                build.Clear();

                AppendLine("using System;");
                AppendLine("using System.Collections.Generic;");
                AppendLine("using System.Linq;");
                AppendLine(string.Format("using {0}.Repository;", projectName));
                AppendLine(string.Format("using {0}.Repository.DataModel;", projectName));
                AppendLine(string.Format("using {0}.Repository.ViewModel;", projectName));
                build.AppendLine();

                AppendLine(string.Format("namespace {0}.Service", this.projectName));
                AppendLine("{");
                AppendLine(string.Format("public class {0}Service : I{0}", projectName), 1);
                AppendLine("{", 1);
                build.AppendLine();

                AppendLine("#region Repository", 2);
                AppendLine(string.Format("private I{0}Repository _repo;", projectName), 2);
                AppendLine(string.Format("private I{0}Repository Repository", projectName), 2);
                AppendLine("{", 2);
                AppendLine("get", 3);
                AppendLine("{", 3);
                AppendLine("if (_repo == null)", 4);
                AppendLine("{", 4);
                AppendLine(string.Format("_repo = new {0}Repository();", projectName), 5);
                AppendLine("}", 4);
                AppendLine("return _repo", 4);
                AppendLine("}", 3);
                AppendLine("}", 2);
                AppendLine("#endregion", 2);
                AppendLine("}", 1);
                AppendLine("}");

                System.IO.File.WriteAllText(filename, build.ToString());
            }

            CreateServiceInterface();
        }

        private void CreateServiceInterface()
        {
            string filename = string.Format(@"{0}\I{1}Service.cs", servicepath, projectName);
            if (!System.IO.File.Exists(filename))
            {
                build.Clear();

                AppendLine("using System;");
                AppendLine("using System.Collections.Generic;");
                AppendLine(string.Format("using {0}.Repository.ViewModel;", projectName));
                AppendLine(string.Format("using {0}.Repository.DataModel;", projectName));
                build.AppendLine();

                AppendLine(string.Format("namespace {0}.Service", this.projectName));
                AppendLine("{");
                AppendLine(string.Format("public interface I{0}Service : I{0}", projectName), 1);
                AppendLine("{", 1);
                AppendLine("}", 1);
                AppendLine("}");

                System.IO.File.WriteAllText(filename, build.ToString());
            }
        }

        public void CreateDabaseContext(string databaseName, DataRow[] entities)
        {
            string filename = string.Format(@"{0}\DatabaseContext.cs", datamodelpath);
            if (!System.IO.File.Exists(filename))
            {
                build.Clear();

                AppendLine("using System;");
                AppendLine("using System.Collections.Generic;");
                AppendLine("using Microsoft.EntityFrameworkCore;");
                build.AppendLine();

                AppendLine(string.Format("namespace {0}.Service", this.projectName));
                AppendLine("{");
                AppendLine(string.Format("public class DatabaseContext : DbContext", projectName), 1);
                AppendLine("{", 1);

                AppendLine(string.Format("private const string connectionstring = \"{0};\"", Database.ConnectionString(databaseName)), 2);
                build.AppendLine();

                foreach (DataRow row in entities)
                {
                    AppendLine(string.Format("public DbSet<{0}> {0}Set ", projectName) + "{ get; set; }", 2);
                    build.AppendLine();
                }

                AppendLine("protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)", 2);
                AppendLine("{", 2);
                AppendLine("optionsBuilder.UseSqlServer(connectionstring);", 3);
                AppendLine("}", 2);

                AppendLine("}", 1);
                AppendLine("}");

                System.IO.File.WriteAllText(filename, build.ToString());
            }
        }

        private void AppendLine(string text, int tabCount = 0)
        {
            for (int i = 0; i < tabCount; i++)
            {
                build.Append("\t");
            }
            build.AppendLine(text);
        }

        private void Append(string text, int tabCount = 0)
        {
            for (int i = 0; i < tabCount; i++)
            {
                build.Append("\t");
            }
            build.Append(text);
        }
    }
}
