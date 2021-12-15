using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
namespace RebelCmsGenerator
{
    internal class CodeGenerator
    {
        private const string DEFAULT_DATABASE = "rebelcms";
        private string connection;
        enum TextCase
        {
            LcWords,
            UcWords
        }
        public class DatabaseMapping
        {
            public string? TableName { get; set; }
        }
        public class DescribeTableModel
        {
            public string? KeyValue { get; init; }
            public string? FieldValue { get; init; }
            public string? TypeValue { get; init; }
            public string? NullValue { get; init; }
            public string? ExtraValue { get; init; }
        }
        public CodeGenerator(string connection1)
        {
            connection = connection1;

        }
        MySqlConnection GetConnection()
        {
            return new MySqlConnection(connection);
        }
        public static List<string> GetStringDataType()
        {
            return new List<string> { "char", "varchar", "text" };
        }
        public static List<string> GetNumberDataType()
        {
            return new List<string> { "tinyinit", "bool", "boolean", "smallint", "int", "integer", "year", "INT", "YEAR", "SMALLINT", "BOOL", "BOOLEAN" };
        }
        public static List<string> GetNumberDotDataType()
        {
            return new List<string> { "decimal", "float", "double" };
        }
        public static List<string> GetDateDataType()
        {
            return new List<string> { "date", "datetime", "timestamp", "time" };
        }

        public static List<string> GetDateFormatUsa()
        {
            return new List<string> {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                     "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                     "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                     "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                     "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
        }
        public static List<string> GetDateFormatNonUsa()
        {
            return new List<string> {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                     "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                     "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                     "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                     "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
        }
        public List<string> GetTableList()
        {
            using MySqlConnection connection = GetConnection();
            connection.Open();
            List<string> tableNames = new();
            string sql = $@"
            SELECT  TABLE_NAME 
            FROM    information_schema.tables 
            WHERE   table_Schema='{DEFAULT_DATABASE}'";
            var command = new MySqlCommand(sql, connection);
            try
            {
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        if (reader["TABLE_NAME"] != null)
                        {
                            string? name = reader["TABLE_NAME"].ToString();
                            if (name != null)
                            {
                                tableNames.Add(name);
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No Record");

                }
                reader.Close();
            }
            catch (MySqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return tableNames;
        }
        private List<DescribeTableModel> GetTableStructure(string tableName)
        {
            using MySqlConnection connection = GetConnection();
            connection.Open();

            List<DescribeTableModel> describeTableModels = new();
            string sql = $@"DESCRIBE  `{tableName}` ";
            var command = new MySqlCommand(sql, connection);
            try
            {
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        describeTableModels.Add(new DescribeTableModel
                        {
                            KeyValue = reader["Key"].ToString(),
                            FieldValue = reader["Field"].ToString(),
                            TypeValue = reader["Type"].ToString(),
                            NullValue = reader["Null"].ToString(),
                            ExtraValue = reader["Extra"].ToString()
                        });
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No Record");

                }

            }
            catch (MySqlException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                command.Dispose();
            }
            return describeTableModels;
        }
        public string GenerateModel(string tableName, string module)
        {
            var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            StringBuilder template = new();
            template.AppendLine($"namespace RebelCmsTemplate.Models.{UpperCaseFirst(module)};");
            template.AppendLine("public class " + GetStringNoUnderScore(tableName, (int)TextCase.UcWords) + "Model");
            template.AppendLine("{");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;

                if (Key.Equals("PRI") || Key.Equals("MUL"))
                {
                    if (Field != null)
                        template.AppendLine("\tpublic int " + UpperCaseFirst(Field.Replace("Id", "Key")) + " { get; init; } ");
                }
                else
                {


                    if (GetNumberDataType().Any(x => Type.Contains(x)))
                    {
                        template.AppendLine("\tpublic int " + UpperCaseFirst(Field) + " { get; init; } ");
                    }
                    else if (Type.Contains("decimal"))
                    {
                        template.AppendLine("\tpublic decimal " + UpperCaseFirst(Field) + " { get; init; } ");
                    }
                    else if (Type.Equals("double") || Type.Equals("float"))
                    {
                        template.AppendLine("\tpublic double " + UpperCaseFirst(Field) + " { get; init; } ");
                    }
                    else if (GetDateDataType().Any(x => Type.Contains(x)))
                    {
                        if (Type.Equals("datetime"))
                        {
                            template.AppendLine("\tpublic DateTime " + UpperCaseFirst(Field) + "  { get; init; } ");
                        }
                        else if (Type.Equals("date"))
                        {
                            template.AppendLine("\tpublic DateTime " + UpperCaseFirst(Field) + " { get; init; } ");
                        }
                        else if (Type.Equals("year"))
                        {
                            template.AppendLine("\tpublic int " + UpperCaseFirst(Field) + " { get; init; } ");
                        }
                        else
                        {
                            template.AppendLine("\tpublic string? " + UpperCaseFirst(Field) + " { get; init; } ");
                        }
                    }
                    else
                    {
                        template.AppendLine("\tpublic string? " + UpperCaseFirst(Field) + " { get; init; } ");
                    }

                }
            }


            template.AppendLine("}");

            return template.ToString();
        }
        public string GenerateController(string tableName, string module)
        {
            var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

            StringBuilder template = new();
            StringBuilder createModelString = new();
            StringBuilder updateModelString = new();
            int counter = 1;
            foreach (var fieldName in fieldNameList)
            {
                var name = string.Empty;
                if (fieldName!= null)
                    name = fieldName;
                if (name.Contains("Id"))
                {
                    name = name.Replace("Id", "Key");
                }
                if (counter == fieldNameList.Count)
                {
                    updateModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)}");
                    createModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)}");
                }
                else
                {
                    if (!name.Equals(lcTableName+"Key"))
                        createModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)},");

                    updateModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)},");
                }
                counter++;
            }
            template.AppendLine("using Microsoft.AspNetCore.Mvc;");
            template.AppendLine("using RebelCmsTemplate.Enum;");
            template.AppendLine($"using RebelCmsTemplate.Models.{module};");
            template.AppendLine($"using RebelCmsTemplate.Repository.{module};");
            template.AppendLine("using RebelCmsTemplate.Util;");
            template.AppendLine("using System.Globalization;");
            template.AppendLine($"namespace RebelCmsTemplate.Controllers.Api.{module};");
            template.AppendLine($"[Route(\"api/{module.ToLower()}/[controller]\")]");
            template.AppendLine("[ApiController]");
            template.AppendLine("public class " + ucTableName + "Controller : Controller {");

            template.AppendLine(" private readonly IHttpContextAccessor _httpContextAccessor;");
            template.AppendLine(" private readonly RenderViewToStringUtil _renderViewToStringUtil;");
            template.AppendLine(" public " + ucTableName + "Controller(RenderViewToStringUtil renderViewToStringUtil, IHttpContextAccessor httpContextAccessor)");
            template.AppendLine(" {");
            template.AppendLine("  _renderViewToStringUtil = renderViewToStringUtil;");
            template.AppendLine("  _httpContextAccessor = httpContextAccessor;");
            template.AppendLine(" }");
            template.AppendLine(" [HttpGet]");
            template.AppendLine(" public async Task<IActionResult> Get()");
            template.AppendLine(" {");
            template.AppendLine("   SharedUtil sharedUtils = new(_httpContextAccessor);");
            template.AppendLine("   if (sharedUtils.GetTenantId() == 0 || sharedUtils.GetTenantId().Equals(null))");
            template.AppendLine("   {");
            template.AppendLine("    const string? templatePath = \"~/Views/Error/403.cshtml\";");
            template.AppendLine("    var page = await _renderViewToStringUtil.RenderViewToStringAsync(ControllerContext, templatePath);");
            template.AppendLine("    return Ok(page);");
            template.AppendLine("   }");
            template.AppendLine($"   {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
            template.AppendLine($"   var content = {lcTableName}Repository.GetExcel();");

            Random random = new();
            var fileName = lcTableName + random.Next(1, 100);

            template.AppendLine($"   return File(content,\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\",\"{fileName}.xlsx\");");
            template.AppendLine("  }");
            template.AppendLine("  [HttpPost]");
            template.AppendLine("  public ActionResult Post()");
            template.AppendLine("  {");
            template.AppendLine("\tvar status = false;");
            template.AppendLine("\tvar mode = Request.Form[\"mode\"];");
            template.AppendLine("\tvar leafCheckKey = Convert.ToInt32(Request.Form[\"leafCheckKey\"]);");

            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;

                if (Field.Contains("Id"))
                    Field = Field.Replace("Id", "Key");

                if (GetNumberDataType().Any(x => Type.Contains(x)))
                {
                    template.AppendLine($"\tint {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToInt32(Request.Form[\"{Field}\"]):0;");
                }
                else if (GetDateDataType().Any(x => Type.Contains(x)))
                {
                    if (Type.ToString().Contains("datetime") || Type.ToString().Contains("date"))
                    {
                        var format = (Type.ToString().Contains("datetime")) ? "yyyy-MM-dd HH: mm" : "yyyy-MM-dd";
                        template.AppendLine($"\tDateTime {Field} = DateTime.MinValue;");
                        template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{Field}\"]))");
                        template.AppendLine("\t{");
                        template.AppendLine($"\tif (DateTime.TryParseExact(Request.Form[\"{Field}\"], \""+format+"\",new CultureInfo(\"en-US\"),DateTimeStyles.None,out "+Field+" ));");
                        template.AppendLine("\t}");
                    }
                    else if (Type.ToString().Contains("year"))
                    {
                        template.AppendLine($"\tint {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToInt32(Request.Form[\"{Field}\"]):0;");
                    }
                    else
                    {
                        template.AppendLine($"\tint {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToInt32(Request.Form[\"{Field}\"]):0;");
                    }
                }
                else if (Type.Contains("double") || Type.Contains("float"))
                {
                    template.AppendLine($"\tdouble {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToDouble(Request.Form[\"{Field}\"]):0;");

                }
                else if (Type.Contains("decimal"))
                {
                    template.AppendLine($"\tdecimal {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToDecimal(Request.Form[\"{Field}\"]):0.00;");

                }
                else
                {
                    template.AppendLine($"\tvar {Field} = Request.Form[\"{Field}\"];");
                }


            }

            // end loop 
            template.AppendLine("            var search = Request.Form[\"search\"];");

            template.AppendLine($"           {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
            template.AppendLine("            SharedUtil sharedUtil = new(_httpContextAccessor);");
            template.AppendLine("            CheckAccessUtil checkAccessUtil = new (_httpContextAccessor);");
            template.AppendLine($"           List<{ucTableName}Model> data = new();");

            template.AppendLine("            string code;");
            template.AppendLine("            var lastInsertKey = 0;");
            template.AppendLine("            switch (mode)");
            template.AppendLine("            {");
            template.AppendLine("                case \"create\":");
            template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.CREATE_ACCESS))");
            template.AppendLine("                    {");
            template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.AppendLine("                    }");
            template.AppendLine("                    else");
            template.AppendLine("                    {");
            template.AppendLine("                        try");
            template.AppendLine("                        {");
            template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
            // start loop
            template.AppendLine("                            {");
            template.Append(createModelString);
            template.AppendLine("                            };");
            // end loop
            template.AppendLine($"                           lastInsertKey = {lcTableName}Repository.Create({lcTableName}Model);");
            template.AppendLine("                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
            template.AppendLine("                            status = true;");
            template.AppendLine("                        }");
            template.AppendLine("                        catch (Exception ex)");
            template.AppendLine("                        {");
            template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.AppendLine("                        }");
            template.AppendLine("                    }");
            template.AppendLine("                    break;");
            template.AppendLine("                case \"read\":");
            template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
            template.AppendLine("                    {");
            template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.AppendLine("                    }");
            template.AppendLine("                    else");
            template.AppendLine("                    {");
            template.AppendLine("                        try");
            template.AppendLine("                        {");
            template.AppendLine($"                           data = {lcTableName}Repository.Read();");
            template.AppendLine("                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
            template.AppendLine("                            status = true;");
            template.AppendLine("                        }");
            template.AppendLine("                        catch (Exception ex)");
            template.AppendLine("                        {");
            template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.AppendLine("                        }");
            template.AppendLine("                    }");
            template.AppendLine("                    break;");
            template.AppendLine("                case \"search\":");
            template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
            template.AppendLine("                    {");
            template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.AppendLine("                    }");
            template.AppendLine("                    else");
            template.AppendLine("                    {");
            template.AppendLine("                        try");
            template.AppendLine("                        {");
            template.AppendLine($"                           data = {lcTableName}Repository.Search(search);");
            template.AppendLine("                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
            template.AppendLine("                            status = true;");
            template.AppendLine("                        }");
            template.AppendLine("                        catch (Exception ex)");
            template.AppendLine("                        {");
            template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.AppendLine("                        }");
            template.AppendLine("                    }");
            template.AppendLine("                    break;");
            template.AppendLine("                case \"update\":");
            template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.UPDATE_ACCESS))");
            template.AppendLine("                    {");
            template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.AppendLine("                    }");
            template.AppendLine("                    else");
            template.AppendLine("                    {");
            template.AppendLine("                        try");
            template.AppendLine("                        {");
            template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
            // start loop
            template.AppendLine("                            {");
            template.Append(updateModelString);
            template.AppendLine("                            };");
            // end loop
            template.AppendLine($"                            {lcTableName}Repository.Update({lcTableName}Model);");
            template.AppendLine("                            code = ((int)ReturnCodeEnum.UPDATE_SUCCESS).ToString();");
            template.AppendLine("                            status = true;");
            template.AppendLine("                        }");
            template.AppendLine("                        catch (Exception ex)");
            template.AppendLine("                        {");
            template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.AppendLine("                        }");
            template.AppendLine("                    }");
            template.AppendLine("                    break;");
            template.AppendLine("                case \"delete\":");
            template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.DELETE_ACCESS))");
            template.AppendLine("                    {");
            template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.AppendLine("                    }");
            template.AppendLine("                    else");
            template.AppendLine("                    {");
            template.AppendLine("                        try");
            template.AppendLine("                        {");
            template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
            // start loop
            template.AppendLine("                            {");
            template.AppendLine($"                                {ucTableName}Key = {lcTableName}Key");
            template.AppendLine("                            };");
            // end loop
            template.AppendLine($"                            {lcTableName}Repository.Delete({lcTableName}Model);");
            template.AppendLine("                            code = ((int)ReturnCodeEnum.DELETE_SUCCESS).ToString();");
            template.AppendLine("                            status = true;");
            template.AppendLine("                        }");
            template.AppendLine("                        catch (Exception ex)");
            template.AppendLine("                        {");
            template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.AppendLine("                        }");
            template.AppendLine("                    }");
            template.AppendLine("                    break;");
            template.AppendLine("                default:");
            template.AppendLine("                    code = ((int)ReturnCodeEnum.ACCESS_DENIED_NO_MODE).ToString();");
            template.AppendLine("                    break;");
            template.AppendLine("            }");
            template.AppendLine("            if (data.Count > 0)");
            template.AppendLine("            {");
            template.AppendLine("                return Ok(new { status, code, data });");
            template.AppendLine("            }");

            template.AppendLine("            return lastInsertKey > 0 ? Ok(new { status, code, lastInsertKey }) : Ok(new { status, code });");
            template.AppendLine("        }");
            template.AppendLine("     ");
            template.AppendLine("    }");

            return template.ToString();
        }
        public string GeneratePages(string tableName, string module)
        {
            var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();
            StringBuilder template = new();

            template.AppendLine("@inject Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor");
            template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
            template.AppendLine("@using RebelCmsTemplate.Models.Shared");
            template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
            template.AppendLine("@using RebelCmsTemplate.Util;");
            template.AppendLine("@using RebelCmsTemplate.Enum;");
            template.AppendLine("@{");
            template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
            template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (Key.Equals("MUL"))
                {
                    // do nothing here 
                    if(!Field.Equals("tenantId"))
                    template.AppendLine($"    List<{LowerCaseFirst(Field.Replace("Id", ""))}Model> {LowerCaseFirst(Field.Replace("Id", ""))}Models = new();");

                }
            }
            template.AppendLine("    try");
            template.AppendLine("    {");
            template.AppendLine($"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
            template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");

            template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (Key.Equals("MUL"))
                {
                    if (!Field.Equals("tenantId"))
                    {
                        template.AppendLine($"       {UpperCaseFirst(Field.Replace("Id", ""))}Repository {LowerCaseFirst(Field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                        template.AppendLine($"       {LowerCaseFirst(Field.Replace("Id", ""))}Models = {LowerCaseFirst(Field.Replace("Id", ""))}Repository.Read();");
                    }

                }
            }

            template.AppendLine("    }");
            template.AppendLine("    catch (Exception ex)");
            template.AppendLine("    {");
            template.AppendLine("        sharedUtils.SetSystemException(ex);");
            template.AppendLine("    }");
            template.AppendLine("    var fileInfo = ViewContext.ExecutingFilePath?.Split(\"/\");");
            template.AppendLine("    var filename = fileInfo != null ? fileInfo[4] : \"\";");
            template.AppendLine("    var name = filename.Split(\".\")[0];");
            template.AppendLine("    NavigationModel navigationModel = sharedUtils.GetNavigation(name);");
            template.AppendLine("}");

            template.AppendLine("    <div class=\"page-title\">");
            template.AppendLine("        <div class=\"row\">");
            template.AppendLine("            <div class=\"col-12 col-md-6 order-md-1 order-last\">");
            template.AppendLine("                <h3>@navigationModel.LeafName</h3>");
            template.AppendLine("            </div>");
            template.AppendLine("            <div class=\"col-12 col-md-6 order-md-2 order-first\">");
            template.AppendLine("                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
            template.AppendLine("                    <ol class=\"breadcrumb\">");
            template.AppendLine("                        <li class=\"breadcrumb-item\">");
            template.AppendLine("                            <a href=\"#\">");
            template.AppendLine("                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
            template.AppendLine("                            </a>");
            template.AppendLine("                        </li>");
            template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
            template.AppendLine("                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
            template.AppendLine("                        </li>");
            template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
            template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
            template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
            template.AppendLine("                        </li>");
            template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
            template.AppendLine("                            <i class=\"fas fa-sign-out-alt\"></i>");
            template.AppendLine("                            <a href=\"/logout\">Logout</a>");
            template.AppendLine("                        </li>");
            template.AppendLine("                    </ol>");
            template.AppendLine("                </nav>");
            template.AppendLine("            </div>");
            template.AppendLine("        </div>");
            template.AppendLine("    </div>");
            template.AppendLine("    <section class=\"content\">");
            template.AppendLine("        <div class=\"container-fluid\">");
            template.AppendLine("            <form class=\"form-horizontal\">");
            template.AppendLine("                <div class=\"card card-primary\">");
            template.AppendLine("                    <div class=\"card-header\">Filter</div>");
            template.AppendLine("                    <div class=\"card-body\">");
            template.AppendLine("                        <div class=\"form-group\">");
            template.AppendLine("                            <div class=\"col-md-2\">");
            template.AppendLine("                                <label for=\"search\">Search</label>");
            template.AppendLine("                            </div>");
            template.AppendLine("                            <div class=\"col-md-10\">");
            template.AppendLine("                                <input name=\"search\" id=\"search\" class=\"form-control\"");
            template.AppendLine("                                    placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\"");
            template.AppendLine("                                  style =\"width: 350px!important;\" />");
            template.AppendLine("                            </div>");
            template.AppendLine("                        </div>");
            template.AppendLine("                    </div>");
            template.AppendLine("                   <div class=\"card-footer\">");
            template.AppendLine("                        <button type=\"button\" class=\"btn btn-info\" onclick=\"searchRecord()\">");
            template.AppendLine("                            <i class=\"fas fa-filter\"></i> Filter");
            template.AppendLine("                        </button>");
            template.AppendLine("                        &nbsp;");
            template.AppendLine("                        <button type=\"button\" class=\"btn btn-warning\" onclick=\"resetRecord()\">");
            template.AppendLine("                            <i class=\"fas fa-power-off\"></i> Reset");
            template.AppendLine("                        </button>");
            template.AppendLine("                    </div>");
            template.AppendLine("                </div>");
            template.AppendLine("                <div class=\"row\">");
            template.AppendLine("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
            template.AppendLine("                </div>");
            template.AppendLine("            </form>");
            template.AppendLine("            <div class=\"row\">");
            template.AppendLine("                <div class=\"col-xs-12 col-sm-12 col-md-12\">");
            template.AppendLine("                    <div class=\"card\">");
            template.AppendLine("                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
            template.AppendLine("                            <thead>");
            template.AppendLine("                                <tr>");
            // loop here
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;
                if (Key.Equals("PRI"))
                {
                    // do nothing here 

                }
                else if (Key.Equals("MUL"))
                {
                    if (!Field.Equals("tenantId"))
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <select name=\"{Field}\" id=\"{Field}\" class=\"form-control\">");
                        template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                        template.AppendLine("                                                {");
                        template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                        template.AppendLine("                                                }");
                        template.AppendLine("                                                else");
                        template.AppendLine("                                                {");
                        template.AppendLine($"                                                foreach (var row"+UpperCaseFirst(Field.Replace("Id", ""))+" in "+LowerCaseFirst(Field.Replace("Id", ""))+"Models)");
                        template.AppendLine("                                                {");
                        template.AppendLine($"                                                   <option value=\"@row"+UpperCaseFirst(Field.Replace("Id", ""))+"."+UpperCaseFirst(Field.Replace("Id", ""))+"yKey\">");
                        template.AppendLine("                                                   @row"+UpperCaseFirst(Field.Replace("Id", ""))+"."+UpperCaseFirst(Field.Replace("Id", ""))+"Name</option>");
                        template.AppendLine("                                                }");
                        template.AppendLine("                                               }");

                        template.AppendLine("                                             </select>");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                }
                else if (GetNumberDataType().Any(x => Type.Contains(x)))
                {
                    template.AppendLine("                                    <td>");
                    template.AppendLine("                                        <label>");
                    template.AppendLine($"                                            <input type=\"number\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                    template.AppendLine("                                        </label>");
                    template.AppendLine("                                    </td>");
                }
                else if (GetDateDataType().Any(x => Type.Contains(x)))
                {
                    if (Type.ToString().Contains("DateTime"))
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <input type=\"datetime-local\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                    else if (Type.ToString().Contains("Date"))
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <input type=\"date\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                    else
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                }
                else
                {
                    template.AppendLine("                                    <td>");
                    template.AppendLine("                                        <label>");
                    template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                    template.AppendLine("                                        </label>");
                    template.AppendLine("                                    </td>");
                }


            }
            // end loop
            template.AppendLine("                                    <td style=\"text-align: center\">");
            template.AppendLine("                                        <Button type=\"button\" class=\"btn btn-info\" onclick=\"createRecord()\">");
            template.AppendLine("                                            <i class=\"fa fa-newspaper\"></i>&nbsp;&nbsp;CREATE");
            template.AppendLine("                                        </Button>");
            template.AppendLine("                                    </td>");
            template.AppendLine("                                </tr>");
            template.AppendLine("                                <tr>");
            template.AppendLine("                                    <th>Name</th>");
            template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
            template.AppendLine("                                </tr>");
            template.AppendLine("                            </thead>");
            template.AppendLine("                            <tbody id=\"tableBody\">");
            template.AppendLine($"                                @foreach (var row in {lcTableName}Models)");
            template.AppendLine("                                {");
            template.AppendLine($"                                    <tr id='role-@row.{ucTableName}Key'>");
            /// loop here 
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;
                if (Key.Equals("PRI"))
                {
                    // do nothing here 

                }
                else if (Key.Equals("MUL"))
                {
                    if (!Field.Equals("tenantId"))
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <select name=\"{Field}\" id=\"{Field}\" class=\"form-control\">");
                        template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                        template.AppendLine("                                                {");
                        template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                        template.AppendLine("                                                }");
                        template.AppendLine("                                                else");
                        template.AppendLine("                                                {");
                        template.AppendLine("foreach (var option in from row"+UpperCaseFirst(Field.Replace("Id", ""))+" in "+LowerCaseFirst(Field.Replace("Id", ""))+"Models");
                        template.AppendLine("                        let selected = row"+UpperCaseFirst(Field.Replace("Id", ""))+" in "+UpperCaseFirst(Field.Replace("Id", ""))+"Key ==");
                        template.AppendLine("                       row."+UpperCaseFirst(Field.Replace("Id",""))+"Key");
                        template.AppendLine("                       select selected ? Html.Raw(\"<option value='\" +");
                        template.AppendLine("                       row"+UpperCaseFirst(Field.Replace("Id", ""))+"."+UpperCaseFirst(Field.Replace("Id", ""))+"Key + \"' selected>\" +");
                        template.AppendLine("                       row"+UpperCaseFirst(Field.Replace("Id", ""))+"."+UpperCaseFirst(Field.Replace("Id", ""))+"Name + \"</option>\") :");
                        template.AppendLine("                       Html.Raw(\"<option value=\\\"\" + row"+UpperCaseFirst(Field.Replace("Id", ""))+"."+UpperCaseFirst(Field.Replace("Id", ""))+"Key +");
                        template.AppendLine("                       \"\\\">\" + row"+UpperCaseFirst(Field.Replace("Id", ""))+"."+UpperCaseFirst(Field.Replace("Id", ""))+"Name + \"</option>\"))");
                       template.AppendLine(" {");
                        template.AppendLine("     @option");
                        template.AppendLine("                                  }");
                        template.AppendLine("                                               }");

                        template.AppendLine("                                             </select>");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }

                }
                else if (GetNumberDataType().Any(x => Type.Contains(x)))
                {
                    template.AppendLine("                                    <td>");
                    template.AppendLine("                                        <label>");
                    template.AppendLine($"                                            <input type=\"number\" name=\"{Field}[]\" id=\"{Field}\"  value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                    template.AppendLine("                                        </label>");
                    template.AppendLine("                                    </td>");
                }
                else if (GetDateDataType().Any(x => Type.Contains(x)))
                {
                    if (Type.ToString().Contains("DateTime"))
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <input type=\"datetime-local\" name=\"{Field}[]\" id=\"{Field}\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                    else if (Type.ToString().Contains("Date"))
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <input type=\"date\" name=\"{Field}[]\" id=\"{Field}\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                    else
                    {
                        template.AppendLine("                                    <td>");
                        template.AppendLine("                                        <label>");
                        template.AppendLine($"                                            <input type=\"text\" name=\"{Field}[]\" id=\"{Field}\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                        template.AppendLine("                                        </label>");
                        template.AppendLine("                                    </td>");
                    }
                }
                else
                {
                    template.AppendLine("                                    <td>");
                    template.AppendLine("                                        <label>");
                    template.AppendLine($"                                            <input type=\"text\" name=\"{Field}[]\" id=\"{Field}\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                    template.AppendLine("                                        </label>");
                    template.AppendLine("                                    </td>");
                }

            }
            // loop here
            template.AppendLine("                                        <td style=\"text-align: center\">");
            template.AppendLine("                                            <div class=\"btn-group\">");
            template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord(@row.{lcTableName}Key)\">");
            template.AppendLine("                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
            template.AppendLine("                                                </Button>");
            template.AppendLine("                                                &nbsp;");
            template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord(@row.{lcTableName}Key)\">");
            template.AppendLine("                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");
            template.AppendLine("                                                </Button>");
            template.AppendLine("                                            </div>");
            template.AppendLine("                                       </td>");
            template.AppendLine("                                    </tr>");
            template.AppendLine("                                }");
            template.AppendLine($"                                @if ({lcTableName}Models.Count == 0)");
            template.AppendLine("                                {");
            template.AppendLine("                                    <tr>");
            template.AppendLine("                                        <td colspan=\"7\" class=\"noRecord\">");
            template.AppendLine("                                           @SharedUtil.NoRecord");
            template.AppendLine("                                        </td>");
            template.AppendLine("                                    </tr>");
            template.AppendLine("                                }");
            template.AppendLine("                            </tbody>");
            template.AppendLine("                        </table>");
            template.AppendLine("                    </div>");
            template.AppendLine("                </div>");
            template.AppendLine("            </div>");
            template.AppendLine("        </div>");
            template.AppendLine("    </section>");
            template.AppendLine("    <script>");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = GetStringNoUnderScore(describeTableModel.FieldValue, (int)TextCase.LcWords);
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;

                // later custom validator 

            }
            StringBuilder templateField = new();
            StringBuilder oneLineTemplateField = new();
            foreach (var fieldName in fieldNameList)
            {
                var name = string.Empty;
                if (fieldName != null)
                    name = fieldName;

                if (name.Contains("Id"))
                {
                    templateField.Append("row."+name.Replace("Id", "Key")+",");
                    oneLineTemplateField.Append("row."+name.Replace("Id", "Key")+",");
                }
                else
                {
                    templateField.Append("row."+name+",");
                    oneLineTemplateField.Append(name+",");
                }

                templateField.Append("row."+fieldName+",");
            };
            template.AppendLine("        function resetRecord() {");
            template.AppendLine("         readRecord();");
            template.AppendLine("         $(\"#search\").val(\"\");");
            template.AppendLine("        }");
            template.AppendLine("        function emptyTemplate() {");
            template.AppendLine("         return\"<tr><td colspan='4'>It's lonely here</td></tr>\";");
            template.AppendLine("        }");
            // remember to one row template here as function name 
            template.AppendLine("        function template("+oneLineTemplateField.ToString().TrimEnd(',')+") {");
            template.AppendLine("            return \"\" +");
            template.AppendLine($"                \"<tr id='{lcTableName}-\" + {ucTableName}Key + \"'>\" +");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;

                if (GetNumberDataType().Any(x => Type.Contains(x)))
                {
                    template.AppendLine("                                    \"<td>\" +");
                    template.AppendLine("                                        \"<label>\" +");
                    template.AppendLine("                                            \"<input type='number' name='"+Field+"[]' id='"+Field+"' value='\"+"+Field+"+\"' class='form-control' />\" +");
                    template.AppendLine("                                        \"</label>\" +");
                    template.AppendLine("                                    \"</td>\" +");
                }
                else if (GetDateDataType().Any(x => Type.Contains(x)))
                {
                    if (Type.ToString().Contains("DateTime"))
                    {
                        template.AppendLine("                                    \"<td>\" +");
                        template.AppendLine("                                        \"<label>\" +");
                        template.AppendLine("                                            \"<input type='datetime-local' name='"+Field+"[]' id='"+Field+"' value='\"+"+Field+"+\"' class='form-control' />\" +");
                        template.AppendLine("                                        \"</label>\" +");
                        template.AppendLine("                                    \"</td>\" +");
                    }
                    else if (Type.ToString().Contains("Date"))
                    {
                        template.AppendLine("                                    \"<td>\" +");
                        template.AppendLine("                                        \"<label>\" +");
                        template.AppendLine("                                            \"<input type='date' name='"+Field+"[]' name='"+Field+"[]' id='"+Field+"' value='\"+"+Field+"+\"' value='@row.{Field}' class='form-control' />\" +");
                        template.AppendLine("                                        \"</label>\" +");
                        template.AppendLine("                                    \"</td>\" +");
                    }
                    else
                    {
                        template.AppendLine("                                    \"<td>\" +");
                        template.AppendLine("                                        \"<label>\" +");
                        template.AppendLine("                                            \"<input type='text' name='"+Field+"[]' id='"+Field+"' value='\"+"+Field+"+\"' class='form-control' />\" +");
                        template.AppendLine("                                        \"</label>");
                        template.AppendLine("                                    \"</td>\" +\" +");
                    }
                }
                else
                {
                    template.AppendLine("                                    \"<td>\" +");
                    template.AppendLine("                                        \"<label>\" +");
                    template.AppendLine($"                                            \"<input type='text' name='"+Field+"[]' id='"+Field+"' value='\"+"+Field+"+\"' class='form-control' />\" +");
                    template.AppendLine("                                        \"</label>\" +");
                    template.AppendLine("                                    \"</td>\" +");
                }

            }
            template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
            template.AppendLine($"                \"<Button type='button' class='btn btn-warning' onclick='updateRecord(\" + {ucTableName}Key + \")'>\" +");
            template.AppendLine("                \"<i class='fas fa-edit'></i> UPDATE\" +");
            template.AppendLine("                \"</Button>\" +");
            template.AppendLine("                \"&nbsp;\" +");
            template.AppendLine($"                \"<Button type='button' class='btn btn-danger' onclick='deleteRecord(\" + {ucTableName}Key + \")'>\" +");
            template.AppendLine("                \"<i class='fas fa-trash'></i> DELETE\" +");
            template.AppendLine("                \"</Button>\" +");
            template.AppendLine("                \"</div></td>\" +");
            template.AppendLine("                \"</tr>\";");
            template.AppendLine("        }");
            template.AppendLine("        function createRecord() {");
            // loop here 
            foreach (var fieldName in fieldNameList)
            {
                var name = string.Empty;
                if (fieldName != null)
                    name  = fieldName;

                if (name.Contains("Id"))
                {
                    template.AppendLine($"         const {name.Replace("Id", "Key")} = $(\"#{name.Replace("Id", "Key")}\");");
                }
                else
                {
                    template.AppendLine($"         const {name} = $(\"#{name}\");");
                }
            }
            template.AppendLine("         const roleName = $(\"#roleName\");");
            // loop here
            template.AppendLine("         $.ajax({");
            template.AppendLine("          type: 'POST',");
            template.AppendLine("           url: \"api/administrator/" + lcTableName + "\",");
            template.AppendLine("           async: false,");
            template.AppendLine("           data: {");
            template.AppendLine("            mode: 'create',");
            template.AppendLine("            leafCheckKey: @navigationModel.LeafCheckKey,");
            // loop here
            foreach (var fieldName in fieldNameList)
            {
                var name = string.Empty;
                if (fieldName != null)
                    name  = fieldName;

                if (name.Contains("Id"))
                {
                    template.AppendLine($"            {name.Replace("Id", "Key")}: {name.Replace("Id", "Key")}.val()");
                }
                else
                {
                    template.AppendLine($"            {name}: {name}.val()");
                }
            }
            // loop here
            template.AppendLine("           },statusCode: {");
            template.AppendLine("            500: function () {");
            template.AppendLine("             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("           }");
            template.AppendLine("          },");
            template.AppendLine("          beforeSend: function () {");
            template.AppendLine("           console.log(\"loading ..\");");
            template.AppendLine("          }}).done(function(data)  {");
            template.AppendLine("            if (data === void 0) {");
            template.AppendLine("             location.href = \"/\";");
            template.AppendLine("            }");
            template.AppendLine("            let status = data.status;");
            template.AppendLine("            let code = data.code;");
            template.AppendLine("            if (status) {");
            template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
            template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey, roleName.val()));");
            template.AppendLine("             Swal.fire({");
            template.AppendLine("               title: 'Success!',");
            template.AppendLine("               text: '@SharedUtil.RecordCreated',");
            template.AppendLine("               icon: 'success',");
            template.AppendLine("               confirmButtonText: 'Cool'");
            template.AppendLine("             });");
            // loop here
            foreach (var fieldName in fieldNameList)
            {
                var name = string.Empty;
                if (fieldName != null)
                    name  = fieldName;

                if (name.Contains("Id"))
                {
                    template.AppendLine($"            {name.Replace("Id", "Key")}: {name.Replace("Id", "Key")}.val('');");
                }
                else
                {
                    template.AppendLine("             "+name+".val('');");
                }
            }
            // loop here
            template.AppendLine("            } else if (status === false) {");
            template.AppendLine("             if (typeof(code) === 'string'){");
            template.AppendLine("             @{");
            template.AppendLine("              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.AppendLine("              {");
            template.AppendLine("               <text>");
            template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.AppendLine("               </text>");
            template.AppendLine("              }else{");
            template.AppendLine("               <text>");
            template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("               </text>");
            template.AppendLine("              }");
            template.AppendLine("             }");
            template.AppendLine("            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.AppendLine("             let timerInterval;");
            template.AppendLine("             Swal.fire({");
            template.AppendLine("              title: 'Auto close alert!',");
            template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.AppendLine("              timer: 2000,");
            template.AppendLine("              timerProgressBar: true,");
            template.AppendLine("              didOpen: () => {");
            template.AppendLine("                Swal.showLoading()");
            template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b')");
            template.AppendLine("                timerInterval = setInterval(() => {");
            template.AppendLine("                b.textContent = Swal.getTimerLeft()");
            template.AppendLine("               }, 100)");
            template.AppendLine("              },");
            template.AppendLine("              willClose: () => {");
            template.AppendLine("               clearInterval(timerInterval)");
            template.AppendLine("              }");
            template.AppendLine("            }).then((result) => {");
            template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
            template.AppendLine("               console.log('session out .. ');");
            template.AppendLine("               location.href = \"/\";");
            template.AppendLine("              }");
            template.AppendLine("            });");
            template.AppendLine("           } else {");
            template.AppendLine("            location.href = \"/\";");
            template.AppendLine("           }");
            template.AppendLine("          } else {");
            template.AppendLine("           location.href = \"/\";");
            template.AppendLine("          }");
            template.AppendLine("         }).fail(function(xhr)  {");
            template.AppendLine("          console.log(xhr.status)");
            template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("         }).always(function (){");
            template.AppendLine("          console.log(\"always:complete\");    ");
            template.AppendLine("         });");
            template.AppendLine("        }");
            template.AppendLine("        function readRecord() {");
            template.AppendLine("         let row = { roleKey: \"\", folderName: \"\" }");
            template.AppendLine("         $.ajax({");
            template.AppendLine("          type: \"post\",");
            template.AppendLine("          url: \"api/administrator/" + lcTableName + "\",");
            template.AppendLine("          async: false,");
            template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
            template.AppendLine("          data: {");
            template.AppendLine("           mode: \"read\",");
            template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
            template.AppendLine("          }, statusCode: {");
            template.AppendLine("           500: function () {");
            template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("           }");
            template.AppendLine("          }, beforeSend() {");
            template.AppendLine("           console.log(\"loading ..\");");
            template.AppendLine("          }}).done(function(data)  {");
            template.AppendLine("              if (data === void 0) {");
            template.AppendLine("               location.href = \"/\";");
            template.AppendLine("              }");
            template.AppendLine("              let status = data.status;");
            template.AppendLine("              let code = data.code;");
            template.AppendLine("              if (status) {");
            template.AppendLine("               if (data.data === void 0) {");
            template.AppendLine("                $(\"#tableBody\").html(\"\").html(emptyTemplate());");
            template.AppendLine("               } else {");
            template.AppendLine("                if (data.data.length > 0) {");
            template.AppendLine("                 let templateStringBuilder = \"\";");
            template.AppendLine("                 for (let i = 0; i < data.data.length; i++) {");
            template.AppendLine("                  row = data.data[i];");
            // remember one line row 
            template.AppendLine("                  templateStringBuilder += template("+templateField.ToString().TrimEnd(',')+");");
            template.AppendLine("                 }");
            template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
            template.AppendLine("                } else {");
            template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
            template.AppendLine("                }");
            template.AppendLine("               }");
            template.AppendLine("              } else if (status === false) {");
            template.AppendLine("               if (typeof(code) === 'string'){");
            template.AppendLine("               @{");
            template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.AppendLine("                {");
            template.AppendLine("                 <text>");
            template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.AppendLine("                 </text>");
            template.AppendLine("                }");
            template.AppendLine("                else");
            template.AppendLine("                {");
            template.AppendLine("                 <text>");
            template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("                 </text>");
            template.AppendLine("                }");
            template.AppendLine("               }");
            template.AppendLine("              }else   if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.AppendLine("               let timerInterval;");
            template.AppendLine("               Swal.fire({");
            template.AppendLine("                title: 'Auto close alert!',");
            template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.AppendLine("                timer: 2000,");
            template.AppendLine("                timerProgressBar: true,");
            template.AppendLine("                didOpen: () => {");
            template.AppendLine("                 Swal.showLoading()");
            template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b')");
            template.AppendLine("                 timerInterval = setInterval(() => {");
            template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
            template.AppendLine("                }, 100)");
            template.AppendLine("               },");
            template.AppendLine("               willClose: () => {");
            template.AppendLine("                clearInterval(timerInterval)");
            template.AppendLine("               }");
            template.AppendLine("              }).then((result) => {");
            template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
            template.AppendLine("                console.log('session out .. ');");
            template.AppendLine("                location.href = \"/\";");
            template.AppendLine("               }");
            template.AppendLine("              });");
            template.AppendLine("            } else {");
            template.AppendLine("             location.href = \"/\";");
            template.AppendLine("            }");
            template.AppendLine("           } else {");
            template.AppendLine("            location.href = \"/\";");
            template.AppendLine("           }");
            template.AppendLine("          }).fail(function(xhr)  {");
            template.AppendLine("           console.log(xhr.status)");
            template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("          }).always(function (){");
            template.AppendLine("           console.log(\"always:complete\");    ");
            template.AppendLine("          });");
            template.AppendLine("        }");
            template.AppendLine("        function searchRecord() {");
            template.AppendLine("         let row = { roleKey: \"\", folderName: \"\" }");
            template.AppendLine("         $.ajax({");
            template.AppendLine("          type: \"post\",");
            template.AppendLine("          url: \"api/administrator/" + lcTableName + "\",");
            template.AppendLine("          async: false,");
            template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
            template.AppendLine("          data: {");
            template.AppendLine("           mode: \"search\",");
            template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
            template.AppendLine("           search: $(\"#search\").val()");
            template.AppendLine("          }, ");
            template.AppendLine("          statusCode: {");
            template.AppendLine("           500: function () {");
            template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("           }");
            template.AppendLine("          }, beforeSend() {");
            template.AppendLine("           console.log(\"loading ..\");");
            template.AppendLine("          }}).done(function(data)  {");
            template.AppendLine("            if (data === void 0) { location.href = \"/\"; }");
            template.AppendLine("             let status =data.status;");
            template.AppendLine("             let code = data.code;");
            template.AppendLine("             if (status) {");
            template.AppendLine("              if (data.data === void 0) {");
            template.AppendLine("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
            template.AppendLine("              } else {");
            template.AppendLine("               if (data.data.length > 0) {");
            template.AppendLine("                let templateStringBuilder = \"\";");
            template.AppendLine("                for (let i = 0; i < data.data.length; i++) {");
            template.AppendLine("                 row = data.data[i];");
            // remember one line row 

            template.AppendLine("                 templateStringBuilder += template("+templateField.ToString().TrimEnd(',')+");");
            template.AppendLine("                }");
            template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
            template.AppendLine("               }");
            template.AppendLine("              }");
            template.AppendLine("             } else if (status === false) {");
            template.AppendLine("              if (typeof(code) === 'string'){");
            template.AppendLine("              @{");
            template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.AppendLine("                {");
            template.AppendLine("                 <text>");
            template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.AppendLine("                 </text>");
            template.AppendLine("                }");
            template.AppendLine("                else");
            template.AppendLine("                {");
            template.AppendLine("                 <text>");
            template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("                 </text>");
            template.AppendLine("                }");
            template.AppendLine("               }");
            template.AppendLine("              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.AppendLine("               let timerInterval;");
            template.AppendLine("               Swal.fire({");
            template.AppendLine("                title: 'Auto close alert!',");
            template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.AppendLine("                timer: 2000,");
            template.AppendLine("                timerProgressBar: true,");
            template.AppendLine("                didOpen: () => {");
            template.AppendLine("                 Swal.showLoading()");
            template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b')");
            template.AppendLine("                 timerInterval = setInterval(() => {");
            template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
            template.AppendLine("                }, 100)");
            template.AppendLine("               },");
            template.AppendLine("               willClose: () => { clearInterval(timerInterval) }");
            template.AppendLine("             }).then((result) => {");
            template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
            template.AppendLine("               console.log('session out .. ');");
            template.AppendLine("               location.href = \"/\";");
            template.AppendLine("              }");
            template.AppendLine("             });");
            template.AppendLine("            } else {");
            template.AppendLine("             location.href = \"/\";");
            template.AppendLine("            }");
            template.AppendLine("           } else {");
            template.AppendLine("            location.href = \"/\";");
            template.AppendLine("           }");
            template.AppendLine("         }).fail(function(xhr)  {");
            template.AppendLine("          console.log(xhr.status)");
            template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("         }).always(function (){");
            template.AppendLine("          console.log(\"always:complete\");    ");
            template.AppendLine("         });");
            template.AppendLine("        }");
            template.AppendLine("        function excelRecord() {");
            template.AppendLine("         window.open(\"api/administrator/" + lcTableName + "\");");
            template.AppendLine("        }");
            template.AppendLine("        function updateRecord(" + lcTableName + "Key) {");
            template.AppendLine("         $.ajax({");
            template.AppendLine("          type: 'POST',");
            template.AppendLine("          url: \"api/administrator/" + lcTableName + "\",");
            template.AppendLine("          async: false,");
            template.AppendLine("          data: {");
            template.AppendLine("           mode: 'update',");
            template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
            // loop here
            template.AppendLine("           " + lcTableName + "Key: " + lcTableName + "Key,");
            // loop not primary
            foreach (var fieldName in fieldNameList)
            {
                var name = string.Empty;
                if (fieldName != null)
                    name  = fieldName;

                if (name.Contains("Id"))
                {
                    if (name != lcTableName+"Key ")
                        template.AppendLine($"           {name}: $(\"#{name.Replace("Id", "Key")}-\" + {name.Replace("Id", "Key")}).val();");
                }
                else
                {
                    template.AppendLine($"           {name}: $(\"#{name}-\" + {name}Key).val();");
                }
            }
            // loop here
            template.AppendLine("          }, statusCode: {");
            template.AppendLine("           500: function () {");
            template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("           }");
            template.AppendLine("          },");
            template.AppendLine("          beforeSend: function () {");
            template.AppendLine("           console.log(\"loading..\");");
            template.AppendLine("          }}).done(function(data)  {");
            template.AppendLine("           if (data === void 0) {");
            template.AppendLine("            location.href = \"/\";");
            template.AppendLine("           }");
            template.AppendLine("           let status = data.status;");
            template.AppendLine("           let code = data.code;");
            template.AppendLine("           if (status) {");
            template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success')");
            template.AppendLine("           } else if (status === false) {");
            template.AppendLine("            if (typeof(code) === 'string'){");
            template.AppendLine("            @{");
            template.AppendLine("             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.AppendLine("              {");
            template.AppendLine("               <text>");
            template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.AppendLine("               </text>");
            template.AppendLine("              }");
            template.AppendLine("              else");
            template.AppendLine("              {");
            template.AppendLine("               <text>");
            template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("               </text>");
            template.AppendLine("              }");
            template.AppendLine("             }");
            template.AppendLine("            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.AppendLine("             let timerInterval");
            template.AppendLine("             Swal.fire({");
            template.AppendLine("              title: 'Auto close alert!',");
            template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.AppendLine("              timer: 2000,");
            template.AppendLine("              timerProgressBar: true,");
            template.AppendLine("              didOpen: () => {");
            template.AppendLine("              Swal.showLoading()");
            template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b')");
            template.AppendLine("               timerInterval = setInterval(() => {");
            template.AppendLine("               b.textContent = Swal.getTimerLeft()");
            template.AppendLine("              }, 100)");
            template.AppendLine("             },");
            template.AppendLine("             willClose: () => {");
            template.AppendLine("              clearInterval(timerInterval)");
            template.AppendLine("             }");
            template.AppendLine("            }).then((result) => {");
            template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
            template.AppendLine("               console.log('session out .. ');");
            template.AppendLine("               location.href = \"/\";");
            template.AppendLine("              }");
            template.AppendLine("             });");
            template.AppendLine("            } else {");
            template.AppendLine("             location.href = \"/\";");
            template.AppendLine("            }");
            template.AppendLine("           } else {");
            template.AppendLine("            location.href = \"/\";");
            template.AppendLine("           }");
            template.AppendLine("          }).fail(function(xhr)  {");
            template.AppendLine("           console.log(xhr.status)");
            template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("          }).always(function (){");
            template.AppendLine("           console.log(\"always:complete\");    ");
            template.AppendLine("          });");
            template.AppendLine("        }");
            template.AppendLine("        function deleteRecord(" + lcTableName + "Key) { ");
            template.AppendLine("         Swal.fire({");
            template.AppendLine("          title: 'Are you sure?',");
            template.AppendLine("          text: \"You won't be able to revert this!\",");
            template.AppendLine("          type: 'warning',");
            template.AppendLine("          showCancelButton: true,");
            template.AppendLine("          confirmButtonText: 'Yes, delete it!',");
            template.AppendLine("          cancelButtonText: 'No, cancel!',");
            template.AppendLine("          reverseButtons: true");
            template.AppendLine("         }).then((result) => {");
            template.AppendLine("          if (result.value) {");
            template.AppendLine("           $.ajax({");
            template.AppendLine("            type: 'POST',");
            template.AppendLine($"            url: \"api/administrator/{lcTableName}\",");
            template.AppendLine("            async: false,");
            template.AppendLine("            data: {");
            template.AppendLine("             mode: 'delete',");
            template.AppendLine("             leafCheckKey: @navigationModel.LeafCheckKey,");
            template.AppendLine("             " + lcTableName + "Key: " + lcTableName + "Key");
            template.AppendLine("            }, statusCode: {");
            template.AppendLine("             500: function () {");
            template.AppendLine("              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("             }");
            template.AppendLine("            },");
            template.AppendLine("            beforeSend: function () {");
            template.AppendLine("             console.log(\"loading..\");");
            template.AppendLine("           }}).done(function(data)  {");
            template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
            template.AppendLine("              let status = data.status;");
            template.AppendLine("              let code = data.code;");
            template.AppendLine("              if (status) {");
            template.AppendLine("               $(\"#" + lcTableName + "-\" + " + lcTableName + "Key).remove();");
            template.AppendLine("               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
            template.AppendLine("              } else if (status === false) {");
            template.AppendLine("               if (typeof(code) === 'string'){");
            template.AppendLine("               @{");
            template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.AppendLine("                {");
            template.AppendLine("                 <text>");
            template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.AppendLine("                 </text>");
            template.AppendLine("                }");
            template.AppendLine("                else");
            template.AppendLine("                {");
            template.AppendLine("                 <text>");
            template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("                 </text>");
            template.AppendLine("                }");
            template.AppendLine("               }");
            template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.AppendLine("               let timerInterval;");
            template.AppendLine("               Swal.fire({");
            template.AppendLine("                title: 'Auto close alert!',");
            template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.AppendLine("                timer: 2000,");
            template.AppendLine("                timerProgressBar: true,");
            template.AppendLine("                didOpen: () => {");
            template.AppendLine("                 Swal.showLoading()");
            template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b')");
            template.AppendLine("                 timerInterval = setInterval(() => {");
            template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
            template.AppendLine("                }, 100)");
            template.AppendLine("               },");
            template.AppendLine("               willClose: () => {");
            template.AppendLine("                clearInterval(timerInterval)");
            template.AppendLine("               }");
            template.AppendLine("             }).then((result) => {");
            template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
            template.AppendLine("                console.log('session out .. ');");
            template.AppendLine("                location.href = \"/\";");
            template.AppendLine("               }");
            template.AppendLine("             });");
            template.AppendLine("            } else {");
            template.AppendLine("             location.href = \"/\";");
            template.AppendLine("            }");
            template.AppendLine("           } else {");
            template.AppendLine("            location.href = \"/\";");
            template.AppendLine("           }");
            template.AppendLine("         }).fail(function(xhr)  {");
            template.AppendLine("           console.log(xhr.status)");
            template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.AppendLine("         }).always(function (){");
            template.AppendLine("          console.log(\"always:complete\");    ");
            template.AppendLine("         });");
            template.AppendLine("       } else if (result.dismiss === swal.DismissReason.cancel) {");
            template.AppendLine("        Swal.fire({");
            template.AppendLine("          icon: 'error',");
            template.AppendLine("          title: 'Cancelled',");
            template.AppendLine("          text: 'Be careful before delete record'");
            template.AppendLine("        })");
            template.AppendLine("       }");
            template.AppendLine("      });");
            template.AppendLine("    }");
            template.AppendLine("    </script>");




            return template.ToString();
        }
        public string GenerateRepository(string tableName, string module)
        {
            var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();
            var sqlFieldName = String.Join(',', fieldNameList);
            List<string?> fieldNameParameter = new();
            foreach (var fieldName in fieldNameList)
            {
                if (fieldName != null)
                {
                    if (fieldName.Equals(lcTableName+"Id"))
                    {
                        fieldNameParameter.Add("null");
                    }
                    else
                    {
                        fieldNameParameter.Add("@"+fieldName);
                    }
                }
            };
            var sqlBindParamFieldName = String.Join(',', fieldNameParameter);

            StringBuilder loopColumn = new();
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;

                List<string> keyValue = new() { "PRI", "MUL" };
                if (keyValue.Contains(Key))
                {
                    if (Key  != "PRI")
                    {
                        if (Field.Equals("tenantId"))
                        {
                            loopColumn.AppendLine("                    new ()");
                            loopColumn.AppendLine("                    {");
                            loopColumn.AppendLine("                        Key = \"@"+Field+"\",");
                            loopColumn.AppendLine("                        Value = _sharedUtil.GetTenantId()");
                            loopColumn.AppendLine("                    },");
                        }
                        else
                        {

                            loopColumn.AppendLine("                    new ()");
                            loopColumn.AppendLine("                    {");
                            loopColumn.AppendLine("                        Key = \"@"+Field+"\",");
                            loopColumn.AppendLine("                        Value = "+lcTableName+"Model."+UpperCaseFirst(Field.Replace("Id", "Key")));
                            loopColumn.AppendLine("                    },");
                        }
                    }
                }
                else
                {
                    if (Field.Equals("isDelete"))
                    {
                        loopColumn.AppendLine("                    new ()");
                        loopColumn.AppendLine("                    {");
                        loopColumn.AppendLine("                        Key = \"@"+Field+"\",");
                        loopColumn.AppendLine("                        Value = 0");
                        loopColumn.AppendLine("                    },");
                    }
                    else if (Field.Equals("executeBy"))
                    {
                        loopColumn.AppendLine("                    new ()");
                        loopColumn.AppendLine("                    {");
                        loopColumn.AppendLine("                        Key = \"@"+Field+"\",");
                        loopColumn.AppendLine("                        Value = _sharedUtil.GetUserName()");
                        loopColumn.AppendLine("                    },");
                    }
                    else
                    {
                        loopColumn.AppendLine("                    new ()");
                        loopColumn.AppendLine("                    {");
                        loopColumn.AppendLine("                        Key = \"@"+Field+"\",");
                        loopColumn.AppendLine("                        Value = "+lcTableName+"Model."+UpperCaseFirst(Field));
                        loopColumn.AppendLine("                    },");
                    }
                }
            }

            StringBuilder template = new();

            template.AppendLine("using System;");
            template.AppendLine("using System.Collections.Generic;");
            template.AppendLine("using System.IO;");
            template.AppendLine("using ClosedXML.Excel;");
            template.AppendLine("using Microsoft.AspNetCore.Http;");
            template.AppendLine("using MySql.Data.MySqlClient;");
            template.AppendLine("using RebelCmsTemplate.Models."+module+";");
            template.AppendLine("using RebelCmsTemplate.Models.Shared;");
            template.AppendLine("using RebelCmsTemplate.Util;");
            template.AppendLine("namespace RebelCmsTemplate.Repository."+module+";");
            template.AppendLine("    public class "+ucTableName+"Repository");
            template.AppendLine("    {");
            template.AppendLine("        private readonly SharedUtil _sharedUtil;");
            template.AppendLine("        public "+ucTableName+"Repository(IHttpContextAccessor httpContextAccessor)");
            template.AppendLine("        {");
            template.AppendLine("            _sharedUtil = new SharedUtil(httpContextAccessor);");
            template.AppendLine("        }");
            template.AppendLine("        public int Create("+ucTableName+"Model "+lcTableName+"Model)");
            template.AppendLine("        {");
            template.AppendLine("            var lastInsertKey = 0;");
            template.AppendLine("            string sql = string.Empty;");
            template.AppendLine("            List<ParameterModel> parameterModels = new ();");
            template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.AppendLine("            try");
            template.AppendLine("            {");
            template.AppendLine("                connection.Open();");
            template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
            template.AppendLine("                sql += @\"INSERT INTO "+tableName+" ("+sqlFieldName+") VALUES ("+sqlBindParamFieldName+");\";");
            template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.AppendLine("                parameterModels = new List<ParameterModel>");

            template.AppendLine("                {");
            // loop start
            template.AppendLine(loopColumn.ToString().TrimEnd(','));
            // loop end
            template.AppendLine("                };");
            template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
            template.AppendLine("                {");
            template.AppendLine("                   mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.AppendLine("                }");
            template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
            template.AppendLine("                mySqlTransaction.Commit();");
            template.AppendLine("                lastInsertKey = (int)mySqlCommand.LastInsertedId;");
            template.AppendLine("                mySqlCommand.Dispose();");
            template.AppendLine("            }");
            template.AppendLine("            catch (MySqlException ex)");
            template.AppendLine("            {");
            template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.AppendLine("                throw new Exception(ex.Message);");
            template.AppendLine("            }");
            template.AppendLine("            return lastInsertKey;");

            template.AppendLine("        }");
            template.AppendLine("        public List<"+ucTableName+"Model> Read()");
            template.AppendLine("        {");
            template.AppendLine("            List<"+ucTableName+"Model> "+lcTableName+"Models = new();");
            template.AppendLine("            string sql = string.Empty;");
            template.AppendLine("            List<ParameterModel> parameterModels = new ();");
            template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.AppendLine("            try");
            template.AppendLine("            {");
            template.AppendLine("                connection.Open();");
            template.AppendLine("                sql = @\"");
            template.AppendLine("                SELECT      *");
            template.AppendLine("                FROM        "+tableName+" ");
            template.AppendLine("                WHERE       isDelete !=1");
            template.AppendLine("                ORDER BY    "+lcTableName+"Id DESC \";");
            template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
            template.AppendLine("                {");
            template.AppendLine("                    while (reader.Read())");
            template.AppendLine("                    {");

            template.AppendLine("                        "+lcTableName+"Models.Add(new "+ucTableName+"Model");
            template.AppendLine("                       {");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;


                if (GetNumberDataType().Any(x => Type.Contains(x)))
                {
                    if (Key.Equals("PRI") || Key.Equals("MUL"))
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field.Replace("Id", "Key")+" = Convert.ToInt32(reader[\""+LowerCaseFirst(Field))+"\"]),");
                    }
                    else
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToInt32(reader[\""+LowerCaseFirst(Field)+"\"]),");
                    }
                }
                else if (Type.Contains("double") || Type.Contains("float"))
                {
                    template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToDouble(reader[\""+LowerCaseFirst(Field)+"\"]),");
                }
                else if (Type.Contains("decimal"))
                {
                    template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToDecimal(reader[\""+LowerCaseFirst(Field)+"\"]),");

                }
                else if (GetDateDataType().Any(x => Type.Contains(x)))
                {

                    if (Type.ToString().Contains("datatime") || Type.ToString().Contains("date"))
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToDateTime(reader[\""+LowerCaseFirst(Field)+"\"]),");
                    }
                    else if (Type.ToString().Contains("year"))
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.DateTime(reader[\""+LowerCaseFirst(Field)+"\"]),");
                    }
                    else
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = (reader[\""+LowerCaseFirst(Field)+"\"].ToString(),");
                    }
                }
                else
                {
                    template.AppendLine("                            "+UpperCaseFirst(Field)+" = reader[\""+LowerCaseFirst(Field)+"\"].ToString(),");
                }

            }
            template.AppendLine("});");
            template.AppendLine("                    }");
            template.AppendLine("                }");
            template.AppendLine("                mySqlCommand.Dispose();");
            template.AppendLine("            }");
            template.AppendLine("            catch (MySqlException ex)");
            template.AppendLine("            {");
            template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.AppendLine("               throw new Exception(ex.Message);");
            template.AppendLine("            }");

            template.AppendLine("            return "+lcTableName+"Models;");
            template.AppendLine("        }");
            template.AppendLine("        public List<"+ucTableName+"Model> Search(string search)");
            template.AppendLine("       {");
            template.AppendLine("            List<" + ucTableName + "Model> " + lcTableName + "Models = new();");
            template.AppendLine("            string sql = string.Empty;");
            template.AppendLine("            List<ParameterModel> parameterModels = new ();");
            template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.AppendLine("            try");
            template.AppendLine("            {");
            template.AppendLine("                connection.Open();");
            template.AppendLine("                sql += @\"");
            template.AppendLine("                SELECT  *");
            template.AppendLine("                FROM    "+tableName+" ");
            template.AppendLine("                WHERE   isDelete != 1");
            template.AppendLine("                AND     tenantName like concat('%',@search,'%'); \";");
            template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.AppendLine("                parameterModels = new List<ParameterModel>");
            template.AppendLine("                {");
            template.AppendLine("                    new ()");
            template.AppendLine("                    {");
            template.AppendLine("                        Key = \"@search\",");
            template.AppendLine("                        Value = search");
            template.AppendLine("                    }");
            template.AppendLine("                };");
            template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
            template.AppendLine("                {");
            template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.AppendLine("                }");
            template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
            template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
            template.AppendLine("                {");
            template.AppendLine("                    while (reader.Read())");
            template.AppendLine("                   {");
            template.AppendLine("                        "+lcTableName+"Models.Add(new "+ucTableName+"Model");
            template.AppendLine("                       {");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string Key = string.Empty;
                string Field = string.Empty;
                string Type = string.Empty;
                if (describeTableModel.KeyValue != null)
                    Key = describeTableModel.KeyValue;
                if (describeTableModel.FieldValue != null)
                    Field = describeTableModel.FieldValue;
                if (describeTableModel.TypeValue != null)
                    Type = describeTableModel.TypeValue;


                if (GetNumberDataType().Any(x => Type.Contains(x)))
                {
                    if (Key.Equals("PRI") || Key.Equals("MUL"))
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field.Replace("Id", "Key")+" = Convert.ToInt32(reader[\""+LowerCaseFirst(Field))+"\"]),");
                    }
                    else
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToInt32(reader[\""+LowerCaseFirst(Field)+"\"]),");
                    }
                }
                else if (Type.Contains("double") || Type.Contains("float"))
                {
                    template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToDouble(reader[\""+LowerCaseFirst(Field)+"\"]),");
                }
                else if (Type.Contains("decimal"))
                {
                    template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToDecimal(reader[\""+LowerCaseFirst(Field)+"\"]),");

                }
                else if (GetDateDataType().Any(x => Type.Contains(x)))
                {

                    if (Type.ToString().Contains("datetime") || Type.ToString().Contains("date"))
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.ToDateTime(reader[\""+LowerCaseFirst(Field)+"\"]),");
                    }
                    else if (Type.ToString().Contains("year"))
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = Convert.DateTime(reader[\""+LowerCaseFirst(Field)+"\"]),");
                    }
                    else
                    {
                        template.AppendLine("                            "+UpperCaseFirst(Field)+" = (reader[\""+LowerCaseFirst(Field)+"\"].ToString(),");
                    }
                }
                else
                {
                    template.AppendLine("                            "+UpperCaseFirst(Field)+" = reader[\""+LowerCaseFirst(Field)+"\"].ToString(),");
                }

            }
            template.AppendLine("});");
            template.AppendLine("                    }");
            template.AppendLine("                }");
            template.AppendLine("                mySqlCommand.Dispose();");
            template.AppendLine("            }");
            template.AppendLine("            catch (MySqlException ex)");
            template.AppendLine("            {");
            template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.AppendLine("                throw new Exception(ex.Message);");
            template.AppendLine("            }");

            template.AppendLine("            return "+lcTableName+"Models;");
            template.AppendLine("        }");
            template.AppendLine("        public byte[] GetExcel()");
            template.AppendLine("        {");
            template.AppendLine("            using var workbook = new XLWorkbook();");
            template.AppendLine("            var worksheet = workbook.Worksheets.Add(\"Administrator > "+ucTableName+" \");");
            // loop here
            for (int i = 0; i < fieldNameList.Count; i++)
            {
                template.AppendLine("            worksheet.Cell(1, "+(i+1)+").Value = \""+fieldNameList[i]+"\";");
            }
            // loop end

            template.AppendLine("            var sql = _sharedUtil.GetSqlSession();");
            template.AppendLine("           var parameterModels = _sharedUtil.GetListSqlParameter();");
            template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.AppendLine("            try");
            template.AppendLine("            {");
            template.AppendLine("               connection.Open();");
            template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.AppendLine("                if (parameterModels != null)");
            template.AppendLine("                {");
            template.AppendLine("                    foreach (ParameterModel parameter in parameterModels)");
            template.AppendLine("                    {");
            template.AppendLine("                        mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.AppendLine("                    }");
            template.AppendLine("                }");
            template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
            template.AppendLine("                {");
            template.AppendLine("                    var counter = 1;");
            template.AppendLine("                   while (reader.Read())");
            template.AppendLine("                    {");
            template.AppendLine("                        var currentRow = counter++;");
            // loop here
            for (int i = 0; i < fieldNameList.Count; i++)
            {
                template.AppendLine("                        worksheet.Cell(currentRow, 2).Value = reader[\""+fieldNameList[i]+"\"].ToString();");
            }
            // loop end here
            template.AppendLine("                    }");
            template.AppendLine("                }");
            template.AppendLine("                mySqlCommand.Dispose();");
            template.AppendLine("            }");
            template.AppendLine("            catch (MySqlException ex)");
            template.AppendLine("            {");
            template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.AppendLine("                throw new Exception(ex.Message);");
            template.AppendLine("            }");
            template.AppendLine("            using var stream = new MemoryStream();");
            template.AppendLine("           workbook.SaveAs(stream);");
            template.AppendLine("            return stream.ToArray();");
            template.AppendLine("        }");
            template.AppendLine("        public void Update(" + ucTableName + "Model " + lcTableName + "Model)");
            template.AppendLine("        {");
            template.AppendLine("            string sql = string.Empty;");
            template.AppendLine("            List<ParameterModel> parameterModels = new ();");
            template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.AppendLine("            try");
            template.AppendLine("            {");
            template.AppendLine("                connection.Open();");
            template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
            template.AppendLine("                sql = @\"");
            template.AppendLine("                UPDATE  "+tableName+" ");
            // start loop
            template.AppendLine("                SET     ");
            StringBuilder updateString = new();
            for (int i = 0; i < fieldNameList.Count; i++)
            {
                if (i+1 == fieldNameList.Count)
                {
                    updateString.AppendLine(fieldNameList[i]+"=@"+fieldNameList[i]);

                }
                else
                {
                    updateString.AppendLine(fieldNameList[i]+"=@"+fieldNameList[i]+",");
                }

            }
            template.AppendLine(updateString.ToString().TrimEnd(','));
            // end loop
            template.AppendLine("                WHERE   " + lcTableName + "Id    =   @" + lcTableName + "Id \";");
            template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");

            // loop end
            template.AppendLine("                parameterModels = new List<ParameterModel>");

            template.AppendLine("                {");
            // loop start
            template.AppendLine(loopColumn.ToString().TrimEnd(','));
            // loop end
            template.AppendLine("                };");
            template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
            template.AppendLine("                {");
            template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.AppendLine("                }");
            template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
            template.AppendLine("                mySqlTransaction.Commit();");
            template.AppendLine("                mySqlCommand.Dispose();");
            template.AppendLine("            }");
            template.AppendLine("            catch (MySqlException ex)");
            template.AppendLine("            {");
            template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.AppendLine("                throw new Exception(ex.Message);");
            template.AppendLine("            }");
            template.AppendLine("        }");
            template.AppendLine("        public void Delete(" + ucTableName + "Model " + lcTableName + "Model)");
            template.AppendLine("        {");
            template.AppendLine("            string sql = string.Empty;");
            template.AppendLine("            List<ParameterModel> parameterModels = new ();");
            template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.AppendLine("            try");
            template.AppendLine("            {");
            template.AppendLine("                connection.Open();");
            template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
            template.AppendLine("                sql = @\"");
            template.AppendLine("                UPDATE  "+tableName+" ");
            template.AppendLine("                SET     isDelete    =   1");
            template.AppendLine("                WHERE   " + lcTableName + "Id    =   "+lcTableName+"Id\";");
            template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.AppendLine("                parameterModels = new List<ParameterModel>");
            template.AppendLine("                {");
            template.AppendLine("                    new ()");
            template.AppendLine("                    {");
            template.AppendLine("                        Key = \"@"+lcTableName+"Id\",");
            template.AppendLine("                        Value = " + lcTableName + "Model." + ucTableName + "Key");
            template.AppendLine("                   }");
            template.AppendLine("                };");
            template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
            template.AppendLine("                {");
            template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.AppendLine("                }");
            template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
            template.AppendLine("                mySqlTransaction.Commit();");
            template.AppendLine("                mySqlCommand.Dispose();");
            template.AppendLine("            }");
            template.AppendLine("            catch (MySqlException ex)");
            template.AppendLine("            {");
            template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.AppendLine("                throw new Exception(ex.Message);");
            template.AppendLine("            }");
            template.AppendLine("        }");
            template.AppendLine("}");

            return template.ToString(); ;
        }

        private static string UpperCaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s[1..];
        }
        private static string LowerCaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToLower(s[0]) + s[1..];
        }
        private static string SplitToUnderScore(string s)
        {
            var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

            return r.Replace(s, "_").ToLower();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="type">1 - uppercase first , 0 - lowercase</param>
        /// <returns></returns>
        private static string GetStringNoUnderScore(string t, int type)
        {
            t = t.ToLower();
            if (t.IndexOf("_") > 0)
            {
                string[] splitTableName = t.Split('_');
                for (int i = 0; i < splitTableName.Length; i++) // Loop with for.
                {
                    if (i > 0)
                    {
                        splitTableName[i] = UpperCaseFirst(splitTableName[i]);
                    }
                }
                t = string.Join("", splitTableName);
            }
            if (type == 1)
            {
                t = UpperCaseFirst(t);
            }
            return t;
        }
    }

}
