using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            return new List<string> { "tinyinit", "bool", "boolean", "smallint", "int", "integer", "year" };
        }
        public static List<string> GetDateDataType()
        {
            return new List<string> { "date", "datetime", "timestamp", "time" };
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
            var ucTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            StringBuilder template = new();
            template.Append($"namespace RebelCmsTemplate.Models.{module};");
            template.Append("public class " + tableName + "Model{");
            foreach (DescribeTableModel describeTableModel in describeTableModels)
            {
                string? Key = describeTableModel.KeyValue;
                var Field = describeTableModel.FieldValue;
                string? DataType = describeTableModel.TypeValue;
                if (Key != null)
                {
                    if (Key.Equals("PRI") || Key.Equals("MUL"))
                    {

                        template.Append("private int " + UpperCaseFirst(Key.Replace("Id", "Key")) + "Key {get,init;}");
                    }
                    else
                    {
                        if (DataType != null)
                        {
                            if (GetNumberDataType().Contains(DataType))
                            {
                                template.Append("private int " + UpperCaseFirst(Key) + " {get,init;}");

                            }
                            else if (GetDateDataType().Contains(DataType))
                            {
                                if (DataType.ToString().Contains("DateTime"))
                                {
                                    template.Append("private DateTime " + UpperCaseFirst(Key) + " {get,init;}");
                                }
                                else
                                {
                                    // some might thing time 20:34  not date but string 
                                    template.Append("private string? " + UpperCaseFirst(Key) + " {get,init;}");
                                }
                            }
                            else
                            {
                                template.Append("private string? " + UpperCaseFirst(Key) + " {get,init;}");
                            }
                        }
                    }
                }
            }


            template.Append('}');

            return template.ToString();
        }
        public string GenerateController(string tableName, string module)
        {
            var ucTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            StringBuilder template = new();

            template.Append($"namespace RebelCmsTemplate.Controllers.Api.{module};\n");
            template.Append($"[Route(\"api/{module.ToLower()}/[controller]\")]");
            template.Append("[ApiController]");
            template.Append("public class " + ucTableName + "Controller : Controller {");

            template.Append(" private readonly IHttpContextAccessor _httpContextAccessor;");
            template.Append(" private readonly RenderViewToStringUtil _renderViewToStringUtil;");
            template.Append(" public RoleController(RenderViewToStringUtil renderViewToStringUtil, IHttpContextAccessor httpContextAccessor)");
            template.Append(" {");
            template.Append("  _renderViewToStringUtil = renderViewToStringUtil;");
            template.Append("  _httpContextAccessor = httpContextAccessor;");
            template.Append(" }");
            template.Append(" [HttpGet]");
            template.Append(" public async Task<IActionResult> Get()");
            template.Append(" {");
            template.Append("   SharedUtil sharedUtils = new(_httpContextAccessor);");
            template.Append("   if (sharedUtils.GetTenantId() == 0 || sharedUtils.GetTenantId().Equals(null))");
            template.Append("   {");
            template.Append("    const string? templatePath = \"~/Views/Error/403.cshtml\";");
            template.Append("    var page = await _renderViewToStringUtil.RenderViewToStringAsync(ControllerContext, templatePath);");
            template.Append("    return Ok(page);");
            template.Append("   }");
            template.Append($"   {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
            template.Append($"   var content = {lcTableName}Repository.GetExcel();");
            template.Append("   return File(content,\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\",\"roles.xlsx\");");
            template.Append("  }");
            template.Append("  [HttpPost]");
            template.Append("  public ActionResult Post()");
            template.Append("  {");
            template.Append("            var status = false;");
            template.Append("            var mode = Request.Form[\"mode\"];");
            template.Append("            var leafCheckKey = Convert.ToInt32(Request.Form[\"leafCheckKey\"]);");
            // here we loop  the best check string is not null 
            template.Append("            var roleName = Request.Form[\"roleName\"];");
            template.Append("            var roleKey = Request.Form[\"roleKey\"];");
            // end loop 
            template.Append("            var search = Request.Form[\"search\"];");

            template.Append($"            {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
            template.Append("            SharedUtil sharedUtil = new(_httpContextAccessor);");
            template.Append("            CheckAccessUtil checkAccessUtil = new (_httpContextAccessor);");
            template.Append($"            List<{ucTableName}Model> data = new();");

            template.Append("            string code;");
            template.Append("            var lastInsertKey = 0;");
            template.Append("            switch (mode)");
            template.Append("            {");
            template.Append("                case \"create\":");
            template.Append("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.CREATE_ACCESS))");
            template.Append("                    {");
            template.Append("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.Append("                    }");
            template.Append("                    else");
            template.Append("                    {");
            template.Append("                        try");
            template.Append("                        {");
            template.Append($"                            {ucTableName}Model {lcTableName}Model = new()");
            // start loop
            template.Append("                            {");
            template.Append("                                RoleName = roleName");
            template.Append("                            };");
            // end loop
            template.Append($"                            lastInsertKey = {lcTableName}Repository.Create(roleModel);");
            template.Append("                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
            template.Append("                            status = true;");
            template.Append("                        }");
            template.Append("                        catch (Exception ex)");
            template.Append("                        {");
            template.Append("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.Append("                        }");
            template.Append("                    }");
            template.Append("                    break;");
            template.Append("                case \"read\":");
            template.Append("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
            template.Append("                    {");
            template.Append("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.Append("                    }");
            template.Append("                    else");
            template.Append("                    {");
            template.Append("                        try");
            template.Append("                        {");
            template.Append($"                            data = {lcTableName}Repository.Read();");
            template.Append("                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
            template.Append("                            status = true;");
            template.Append("                        }");
            template.Append("                        catch (Exception ex)");
            template.Append("                        {");
            template.Append("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.Append("                        }");
            template.Append("                    }");
            template.Append("                    break;");
            template.Append("                case \"search\":");
            template.Append("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
            template.Append("                    {");
            template.Append("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.Append("                    }");
            template.Append("                    else");
            template.Append("                    {");
            template.Append("                        try");
            template.Append("                        {");
            template.Append($"                            data = {lcTableName}Repository.Search(search);");
            template.Append("                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
            template.Append("                            status = true;");
            template.Append("                        }");
            template.Append("                        catch (Exception ex)");
            template.Append("                        {");
            template.Append("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.Append("                        }");
            template.Append("                    }");
            template.Append("                    break;");
            template.Append("                case \"update\":");
            template.Append("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.UPDATE_ACCESS))");
            template.Append("                    {");
            template.Append("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.Append("                    }");
            template.Append("                    else");
            template.Append("                    {");
            template.Append("                        try");
            template.Append("                        {");
            template.Append($"                            {ucTableName}Model {lcTableName}Model = new()");
            // start loop
            template.Append("                            {");
            template.Append("                                RoleName = roleName,");
            template.Append("                                RoleKey = Convert.ToInt32(roleKey)");
            template.Append("                            };");
            // end loop
            template.Append($"                            {lcTableName}Repository.Update(roleModel);");
            template.Append("                            code = ((int)ReturnCodeEnum.UPDATE_SUCCESS).ToString();");
            template.Append("                            status = true;");
            template.Append("                        }");
            template.Append("                        catch (Exception ex)");
            template.Append("                        {");
            template.Append("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.Append("                        }");
            template.Append("                    }");
            template.Append("                    break;");
            template.Append("                case \"delete\":");
            template.Append("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.DELETE_ACCESS))");
            template.Append("                    {");
            template.Append("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
            template.Append("                    }");
            template.Append("                    else");
            template.Append("                    {");
            template.Append("                        try");
            template.Append("                        {");
            template.Append($"                            {ucTableName}Model {lcTableName}Model = new()");
            // start loop
            template.Append("                            {");
            template.Append($"                                {ucTableName}Key = Convert.ToInt32({lcTableName}Key)");
            template.Append("                            };");
            // end loop
            template.Append($"                            {lcTableName}Repository.Delete(roleModel);");
            template.Append("                            code = ((int)ReturnCodeEnum.DELETE_SUCCESS).ToString();");
            template.Append("                            status = true;");
            template.Append("                        }");
            template.Append("                        catch (Exception ex)");
            template.Append("                        {");
            template.Append("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
            template.Append("                        }");
            template.Append("                    }");
            template.Append("                    break;");
            template.Append("                default:");
            template.Append("                    code = ((int)ReturnCodeEnum.ACCESS_DENIED_NO_MODE).ToString();");
            template.Append("                    break;");
            template.Append("            }");
            template.Append("            if (data.Count > 0)");
            template.Append("            {");
            template.Append("                return Ok(new { status, code, data });");
            template.Append("            }");

            template.Append("            return lastInsertKey > 0 ? Ok(new { status, code, lastInsertKey }) : Ok(new { status, code });");
            template.Append("        }");
            template.Append("     ");
            template.Append("    }");
            template.Append('}');

            return template.ToString();
        }
        public string GeneratePages(string tableName, string module)
        {
            var ucTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            StringBuilder template = new();

            template.Append("@inject Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor");
            template.Append($"@using RebelCmsTemplate.Models.{module}");
            template.Append("@using RebelCmsTemplate.Models.Shared");
            template.Append($"@using RebelCmsTemplate.Repository.{module}");
            template.Append("@using RebelCmsTemplate.Util;");
            template.Append("@using RebelCmsTemplate.Enum;");
            template.Append("@{");
            template.Append("    SharedUtil sharedUtils = new(_httpContextAccessor);");
            template.Append($"    List<{ucTableName}Model> {lcTableName}Models = new();");
            template.Append("    try");
            template.Append("    {");
            template.Append($"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
            template.Append($"       {lcTableName}Models = {lcTableName}Repository.Read();");
            template.Append("    }");
            template.Append("    catch (Exception ex)");
            template.Append("    {");
            template.Append("        sharedUtils.SetSystemException(ex);");
            template.Append("    }");
            template.Append("    var fileInfo = ViewContext.ExecutingFilePath?.Split(\"/\");");
            template.Append("    var filename = fileInfo != null ? fileInfo[4] : \"\";");
            template.Append("    var name = filename.Split(\".\")[0];");
            template.Append("    NavigationModel navigationModel = sharedUtils.GetNavigation(name);");
            template.Append("}");

            template.Append("    <div class=\"page-title\">");
            template.Append("        <div class=\"row\">");
            template.Append("            <div class=\"col-12 col-md-6 order-md-1 order-last\">");
            template.Append("                <h3>@navigationModel.LeafName</h3>");
            template.Append("            </div>");
            template.Append("            <div class=\"col-12 col-md-6 order-md-2 order-first\">");
            template.Append("                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
            template.Append("                    <ol class=\"breadcrumb\">");
            template.Append("                        <li class=\"breadcrumb-item\">");
            template.Append("                            <a href=\"#\">");
            template.Append("                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
            template.Append("                            </a>");
            template.Append("                        </li>");
            template.Append("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
            template.Append("                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
            template.Append("                        </li>");
            template.Append("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
            template.Append("                            <i class=\"fas fa-file-excel\"></i>");
            template.Append("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
            template.Append("                        </li>");
            template.Append("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
            template.Append("                            <i class=\"fas fa-sign-out-alt\"></i>");
            template.Append("                            <a href=\"/logout\">Logout</a>");
            template.Append("                        </li>");
            template.Append("                    </ol>");
            template.Append("                </nav>");
            template.Append("            </div>");
            template.Append("        </div>");
            template.Append("    </div>");
            template.Append("    <section class=\"content\">");
            template.Append("        <div class=\"container-fluid\">");
            template.Append("            <form class=\"form-horizontal\">");
            template.Append("                <div class=\"card card-primary\">");
            template.Append("                    <div class=\"card-header\">Filter</div>");
            template.Append("                    <div class=\"card-body\">");
            template.Append("                        <div class=\"form-group\">");
            template.Append("                            <div class=\"col-md-2\">");
            template.Append("                                <label for=\"search\">Search</label>");
            template.Append("                            </div>");
            template.Append("                            <div class=\"col-md-10\">");
            template.Append("                                <input name=\"search\" id=\"search\" class=\"form-control\"");
            template.Append("                                    placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\"");
            template.Append("                                  style =\"width: 350px!important;\" />");
            template.Append("                            </div>");
            template.Append("                        </div>");
            template.Append("                    </div>");
            template.Append("                   <div class=\"card-footer\">");
            template.Append("                        <button type=\"button\" class=\"btn btn-info\" onclick=\"searchRecord()\">");
            template.Append("                            <i class=\"fas fa-filter\"></i> Filter");
            template.Append("                        </button>");
            template.Append("                        &nbsp;");
            template.Append("                        <button type=\"button\" class=\"btn btn-warning\" onclick=\"resetRecord()\">");
            template.Append("                            <i class=\"fas fa-power-off\"></i> Reset");
            template.Append("                        </button>");
            template.Append("                    </div>");
            template.Append("                </div>");
            template.Append("                <div class=\"row\">");
            template.Append("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
            template.Append("                </div>");
            template.Append("            </form>");
            template.Append("            <div class=\"row\">");
            template.Append("                <div class=\"col-xs-12 col-sm-12 col-md-12\">");
            template.Append("                    <div class=\"card\">");
            template.Append("                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
            template.Append("                            <thead>");
            template.Append("                                <tr>");
            template.Append("                                    <td>");
            template.Append("                                        <label>");
            template.Append("                                            <input type=\"text\" name=\"roleName\" id=\"roleName\" class=\"form-control\" />");
            template.Append("                                        </label>");
            template.Append("                                    </td>");
            template.Append("                                    <td style=\"text-align: center\">");
            template.Append("                                        <Button type=\"button\" class=\"btn btn-info\" onclick=\"createRecord()\">");
            template.Append("                                            <i class=\"fa fa-newspaper\"></i>&nbsp;&nbsp;CREATE");
            template.Append("                                        </Button>");
            template.Append("                                    </td>");
            template.Append("                                </tr>");
            template.Append("                                <tr>");
            template.Append("                                    <th>Name</th>");
            template.Append("                                    <th style=\"width: 230px\">Process</th>");
            template.Append("                                </tr>");
            template.Append("                            </thead>");
            template.Append("                            <tbody id=\"tableBody\">");
            template.Append($"                                @foreach (var row in {ucTableName}Models)");
            template.Append("                                {");
            template.Append($"                                    <tr id='role-@row.{lcTableName}Key'>");
            /// loop here 
            template.Append("                                        <td>");
            template.Append("                                            <label>");
            template.Append("                                               <input type=\"text\" class=\"form-control\" name=\"roleName[]\"");
            template.Append("                                                id=\"roleName-@row.RoleKey\" value=\"@row.RoleName\" />");
            template.Append("                                            </label>");
            template.Append("                                        </td>");
            // loop here
            template.Append("                                        <td style=\"text-align: center\">");
            template.Append("                                            <div class=\"btn-group\">");
            template.Append($"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord(@row.{lcTableName}Key)\">");
            template.Append("                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
            template.Append("                                                </Button>");
            template.Append("                                                &nbsp;");
            template.Append($"                                                <Button type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord(@row.{lcTableName}Key)\">");
            template.Append("                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");
            template.Append("                                                </Button>");
            template.Append("                                            </div>");
            template.Append("                                       </td>");
            template.Append("                                    </tr>");
            template.Append("                                }");
            template.Append("                                @if (roleModels.Count == 0)");
            template.Append("                                {");
            template.Append("                                    <tr>");
            template.Append("                                        <td colspan=\"7\" class=\"noRecord\">");
            template.Append("                                           @SharedUtil.NoRecord");
            template.Append("                                        </td>");
            template.Append("                                    </tr>");
            template.Append("                                }");
            template.Append("                            </tbody>");
            template.Append("                        </table>");
            template.Append("                    </div>");
            template.Append("                </div>");
            template.Append("            </div>");
            template.Append("        </div>");
            template.Append("    </section>");
            template.Append("    <script>");
            template.Append("        function resetRecord() {");
            template.Append("         readRecord();");
            template.Append("         $(\"#search\").val(\"\");");
            template.Append("        }");
            template.Append("        function emptyTemplate() {");
            template.Append("         return\"<tr><td colspan='4'>It's lonely here</td></tr>\";");
            template.Append("        }");
            // remember to one row template here as function name 
            template.Append("        function template(roleKey, roleName) {");
            template.Append("            return \"\" +");
            template.Append($"                \"<tr id='{lcTableName}-\" + {ucTableName}Key + \"'>\" +");
            template.Append("                \"<td>     \" +");
            template.Append("                \"<label>\" +");
            template.Append("                \"<input type='text' class='form-control' name='roleName[]' id='roleName-\" + roleKey + \"' value='\" + roleName + \"' />\" +");
            template.Append("                \"</label>\" +");
            template.Append("                \"</td>\" +");
            template.Append("                \"<td style='text-align: center'><div class='btn-group'>\" +");
            template.Append($"                \"<Button type='button' class='btn btn-warning' onclick='updateRecord(\" + {ucTableName}Key + \")'>\" +");
            template.Append("                \"<i class='fas fa-edit'></i> UPDATE\" +");
            template.Append("                \"</Button>\" +");
            template.Append("                \"&nbsp;\" +");
            template.Append($"                \"<Button type='button' class='btn btn-danger' onclick='deleteRecord(\" + {ucTableName}Key + \")'>\" +");
            template.Append("                \"<i class='fas fa-trash'></i> DELETE\" +");
            template.Append("                \"</Button>\" +");
            template.Append("                \"</div></td>\" +");
            template.Append("                \"</tr>\";");
            template.Append("        }");
            template.Append("        function createRecord() {");
            // loop here 
            template.Append("         const roleName = $(\"#roleName\");");
            // loop here
            template.Append("         $.ajax({");
            template.Append("          type: 'POST',");
            template.Append("           url: \"api/administrator/" + lcTableName + "\",");
            template.Append("           async: false,");
            template.Append("           data: {");
            template.Append("            mode: 'create',");
            template.Append("            leafCheckKey: @navigationModel.LeafCheckKey,");
            // loop here
            template.Append("            roleName: roleName.val()");
            // loop here
            template.Append("           },statusCode: {");
            template.Append("            500: function () {");
            template.Append("             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("           }");
            template.Append("          },");
            template.Append("          beforeSend: function () {");
            template.Append("           console.log(\"loading ..\");");
            template.Append("          }}).done(function(data)  {");
            template.Append("            if (data === void 0) {");
            template.Append("             location.href = \"/\";");
            template.Append("            }");
            template.Append("            let status = data.status;");
            template.Append("            let code = data.code;");
            template.Append("            if (status) {");
            template.Append("             const lastInsertKey = data.lastInsertKey;");
            template.Append("             $(\"#tableBody\").prepend(template(lastInsertKey, roleName.val()));");
            template.Append("             Swal.fire({");
            template.Append("               title: 'Success!',");
            template.Append("               text: '@SharedUtil.RecordCreated',");
            template.Append("               icon: 'success',");
            template.Append("               confirmButtonText: 'Cool'");
            template.Append("             });");
            // loop here
            template.Append("             roleName.val('');");
            // loop here
            template.Append("            } else if (status === false) {");
            template.Append("             if (typeof(code) === 'string'){");
            template.Append("             @{");
            template.Append("              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.Append("              {");
            template.Append("               <text>");
            template.Append("                Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.Append("               </text>");
            template.Append("              }else{");
            template.Append("               <text>");
            template.Append("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("               </text>");
            template.Append("              }");
            template.Append("             }");
            template.Append("            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.Append("             let timerInterval;");
            template.Append("             Swal.fire({");
            template.Append("              title: 'Auto close alert!',");
            template.Append("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.Append("              timer: 2000,");
            template.Append("              timerProgressBar: true,");
            template.Append("              didOpen: () => {");
            template.Append("                Swal.showLoading()");
            template.Append("                const b = Swal.getHtmlContainer().querySelector('b')");
            template.Append("                timerInterval = setInterval(() => {");
            template.Append("                b.textContent = Swal.getTimerLeft()");
            template.Append("               }, 100)");
            template.Append("              },");
            template.Append("              willClose: () => {");
            template.Append("               clearInterval(timerInterval)");
            template.Append("              }");
            template.Append("            }).then((result) => {");
            template.Append("              if (result.dismiss === Swal.DismissReason.timer) {");
            template.Append("               console.log('session out .. ');");
            template.Append("               location.href = \"/\";");
            template.Append("              }");
            template.Append("            });");
            template.Append("           } else {");
            template.Append("            location.href = \"/\";");
            template.Append("           }");
            template.Append("          } else {");
            template.Append("           location.href = \"/\";");
            template.Append("          }");
            template.Append("         }).fail(function(xhr)  {");
            template.Append("          console.log(xhr.status)");
            template.Append("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("         }).always(function (){");
            template.Append("          console.log(\"always:complete\");    ");
            template.Append("         });");
            template.Append("        }");
            template.Append("        function readRecord() {");
            template.Append("         let row = { roleKey: \"\", folderName: \"\" }");
            template.Append("         $.ajax({");
            template.Append("          type: \"post\",");
            template.Append("          url: \"api/administrator/" + lcTableName + "\",");
            template.Append("          async: false,");
            template.Append("          contentType: \"application/x-www-form-urlencoded\",");
            template.Append("          data: {");
            template.Append("           mode: \"read\",");
            template.Append("           leafCheckKey: @navigationModel.LeafCheckKey,");
            template.Append("          }, statusCode: {");
            template.Append("           500: function () {");
            template.Append("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("           }");
            template.Append("          }, beforeSend() {");
            template.Append("           console.log(\"loading ..\");");
            template.Append("          }}).done(function(data)  {");
            template.Append("              if (data === void 0) {");
            template.Append("               location.href = \"/\";");
            template.Append("              }");
            template.Append("              let status = data.status;");
            template.Append("              let code = data.code;");
            template.Append("              if (status) {");
            template.Append("               if (data.data === void 0) {");
            template.Append("                $(\"#tableBody\").html(\"\").html(emptyTemplate());");
            template.Append("               } else {");
            template.Append("                if (data.data.length > 0) {");
            template.Append("                 let templateStringBuilder = \"\";");
            template.Append("                 for (let i = 0; i < data.data.length; i++) {");
            template.Append("                  row = data.data[i];");
            // remember one line row 
            template.Append("                  templateStringBuilder += template(row.roleKey, row.roleName);");
            template.Append("                 }");
            template.Append("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
            template.Append("                } else {");
            template.Append("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
            template.Append("                }");
            template.Append("               }");
            template.Append("              } else if (status === false) {");
            template.Append("               if (typeof(code) === 'string'){");
            template.Append("               @{");
            template.Append("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.Append("                {");
            template.Append("                 <text>");
            template.Append("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.Append("                 </text>");
            template.Append("                }");
            template.Append("                else");
            template.Append("                {");
            template.Append("                 <text>");
            template.Append("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("                 </text>");
            template.Append("                }");
            template.Append("               }");
            template.Append("              }else   if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.Append("               let timerInterval;");
            template.Append("               Swal.fire({");
            template.Append("                title: 'Auto close alert!',");
            template.Append("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.Append("                timer: 2000,");
            template.Append("                timerProgressBar: true,");
            template.Append("                didOpen: () => {");
            template.Append("                 Swal.showLoading()");
            template.Append("                 const b = Swal.getHtmlContainer().querySelector('b')");
            template.Append("                 timerInterval = setInterval(() => {");
            template.Append("                 b.textContent = Swal.getTimerLeft()");
            template.Append("                }, 100)");
            template.Append("               },");
            template.Append("               willClose: () => {");
            template.Append("                clearInterval(timerInterval)");
            template.Append("               }");
            template.Append("              }).then((result) => {");
            template.Append("               if (result.dismiss === Swal.DismissReason.timer) {");
            template.Append("                console.log('session out .. ');");
            template.Append("                location.href = \"/\";");
            template.Append("               }");
            template.Append("              });");
            template.Append("            } else {");
            template.Append("             location.href = \"/\";");
            template.Append("            }");
            template.Append("           } else {");
            template.Append("            location.href = \"/\";");
            template.Append("           }");
            template.Append("          }).fail(function(xhr)  {");
            template.Append("           console.log(xhr.status)");
            template.Append("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("          }).always(function (){");
            template.Append("           console.log(\"always:complete\");    ");
            template.Append("          });");
            template.Append("        }");
            template.Append("        function searchRecord() {");
            template.Append("         let row = { roleKey: \"\", folderName: \"\" }");
            template.Append("         $.ajax({");
            template.Append("          type: \"post\",");
            template.Append("          url: \"api/administrator/role\",");
            template.Append("          async: false,");
            template.Append("          contentType: \"application/x-www-form-urlencoded\",");
            template.Append("          data: {");
            template.Append("           mode: \"search\",");
            template.Append("           leafCheckKey: @navigationModel.LeafCheckKey,");
            template.Append("           search: $(\"#search\").val()");
            template.Append("          }, ");
            template.Append("          statusCode: {");
            template.Append("           500: function () {");
            template.Append("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("           }");
            template.Append("          }, beforeSend() {");
            template.Append("           console.log(\"loading ..\");");
            template.Append("          }}).done(function(data)  {");
            template.Append("            if (data === void 0) { location.href = \"/\"; }");
            template.Append("             let status =data.status;");
            template.Append("             let code = data.code;");
            template.Append("             if (status) {");
            template.Append("              if (data.data === void 0) {");
            template.Append("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
            template.Append("              } else {");
            template.Append("               if (data.data.length > 0) {");
            template.Append("                let templateStringBuilder = \"\";");
            template.Append("                for (let i = 0; i < data.data.length; i++) {");
            template.Append("                 row = data.data[i];");
            // remember one line row 
            template.Append("                 templateStringBuilder += template(row.roleKey, row.roleName);");
            template.Append("                }");
            template.Append("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
            template.Append("               }");
            template.Append("              }");
            template.Append("             } else if (status === false) {");
            template.Append("              if (typeof(code) === 'string'){");
            template.Append("              @{");
            template.Append("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.Append("                {");
            template.Append("                 <text>");
            template.Append("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.Append("                 </text>");
            template.Append("                }");
            template.Append("                else");
            template.Append("                {");
            template.Append("                 <text>");
            template.Append("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("                 </text>");
            template.Append("                }");
            template.Append("               }");
            template.Append("              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.Append("               let timerInterval;");
            template.Append("               Swal.fire({");
            template.Append("                title: 'Auto close alert!',");
            template.Append("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.Append("                timer: 2000,");
            template.Append("                timerProgressBar: true,");
            template.Append("                didOpen: () => {");
            template.Append("                 Swal.showLoading()");
            template.Append("                 const b = Swal.getHtmlContainer().querySelector('b')");
            template.Append("                 timerInterval = setInterval(() => {");
            template.Append("                 b.textContent = Swal.getTimerLeft()");
            template.Append("                }, 100)");
            template.Append("               },");
            template.Append("               willClose: () => { clearInterval(timerInterval) }");
            template.Append("             }).then((result) => {");
            template.Append("              if (result.dismiss === Swal.DismissReason.timer) {");
            template.Append("               console.log('session out .. ');");
            template.Append("               location.href = \"/\";");
            template.Append("              }");
            template.Append("             });");
            template.Append("            } else {");
            template.Append("             location.href = \"/\";");
            template.Append("            }");
            template.Append("           } else {");
            template.Append("            location.href = \"/\";");
            template.Append("           }");
            template.Append("         }).fail(function(xhr)  {");
            template.Append("          console.log(xhr.status)");
            template.Append("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("         }).always(function (){");
            template.Append("          console.log(\"always:complete\");    ");
            template.Append("         });");
            template.Append("        }");
            template.Append("        function excelRecord() {");
            template.Append("         window.open(\"api/administrator/role\");");
            template.Append("        }");
            template.Append("        function updateRecord(roleKey) {");
            template.Append("         $.ajax({");
            template.Append("          type: 'POST',");
            template.Append("          url: \"api/administrator/" + lcTableName + "\",");
            template.Append("          async: false,");
            template.Append("          data: {");
            template.Append("           mode: 'update',");
            template.Append("           leafCheckKey: @navigationModel.LeafCheckKey,");
            // loop here
            template.Append("           roleKey: roleKey,");

            template.Append("           roleName: $(\"#roleName-\" + roleKey).val()");
            // loop here
            template.Append("          }, statusCode: {");
            template.Append("           500: function () {");
            template.Append("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("           }");
            template.Append("          },");
            template.Append("          beforeSend: function () {");
            template.Append("           console.log(\"loading..\");");
            template.Append("          }}).done(function(data)  {");
            template.Append("           if (data === void 0) {");
            template.Append("            location.href = \"/\";");
            template.Append("           }");
            template.Append("           let status = data.status;");
            template.Append("           let code = data.code;");
            template.Append("           if (status) {");
            template.Append("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success')");
            template.Append("           } else if (status === false) {");
            template.Append("            if (typeof(code) === 'string'){");
            template.Append("            @{");
            template.Append("             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.Append("              {");
            template.Append("               <text>");
            template.Append("                Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.Append("               </text>");
            template.Append("              }");
            template.Append("              else");
            template.Append("              {");
            template.Append("               <text>");
            template.Append("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("               </text>");
            template.Append("              }");
            template.Append("             }");
            template.Append("            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.Append("             let timerInterval");
            template.Append("             Swal.fire({");
            template.Append("              title: 'Auto close alert!',");
            template.Append("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.Append("              timer: 2000,");
            template.Append("              timerProgressBar: true,");
            template.Append("              didOpen: () => {");
            template.Append("              Swal.showLoading()");
            template.Append("               const b = Swal.getHtmlContainer().querySelector('b')");
            template.Append("               timerInterval = setInterval(() => {");
            template.Append("               b.textContent = Swal.getTimerLeft()");
            template.Append("              }, 100)");
            template.Append("             },");
            template.Append("             willClose: () => {");
            template.Append("              clearInterval(timerInterval)");
            template.Append("             }");
            template.Append("            }).then((result) => {");
            template.Append("              if (result.dismiss === Swal.DismissReason.timer) {");
            template.Append("               console.log('session out .. ');");
            template.Append("               location.href = \"/\";");
            template.Append("              }");
            template.Append("             });");
            template.Append("            } else {");
            template.Append("             location.href = \"/\";");
            template.Append("            }");
            template.Append("           } else {");
            template.Append("            location.href = \"/\";");
            template.Append("           }");
            template.Append("          }).fail(function(xhr)  {");
            template.Append("           console.log(xhr.status)");
            template.Append("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("          }).always(function (){");
            template.Append("           console.log(\"always:complete\");    ");
            template.Append("          });");
            template.Append("        }");
            template.Append("        function deleteRecord(" + lcTableName + "Key) { ");
            template.Append("         Swal.fire({");
            template.Append("          title: 'Are you sure?',");
            template.Append("          text: \"You won't be able to revert this!\",");
            template.Append("          type: 'warning',");
            template.Append("          showCancelButton: true,");
            template.Append("          confirmButtonText: 'Yes, delete it!',");
            template.Append("          cancelButtonText: 'No, cancel!',");
            template.Append("          reverseButtons: true");
            template.Append("         }).then((result) => {");
            template.Append("          if (result.value) {");
            template.Append("           $.ajax({");
            template.Append("            type: 'POST',");
            template.Append("            url: \"api/administrator/role\",");
            template.Append("            async: false,");
            template.Append("            data: {");
            template.Append("             mode: 'delete',");
            template.Append("             leafCheckKey: @navigationModel.LeafCheckKey,");
            template.Append("             " + lcTableName + "Key: " + lcTableName + "Key");
            template.Append("            }, statusCode: {");
            template.Append("             500: function () {");
            template.Append("              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("             }");
            template.Append("            },");
            template.Append("            beforeSend: function () {");
            template.Append("             console.log(\"loading..\");");
            template.Append("           }}).done(function(data)  {");
            template.Append("              if (data === void 0) { location.href = \"/\"; }");
            template.Append("              let status = data.status;");
            template.Append("              let code = data.code;");
            template.Append("              if (status) {");
            template.Append("               $(\"#role-\" + roleKey).remove();");
            template.Append("               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
            template.Append("              } else if (status === false) {");
            template.Append("               if (typeof(code) === 'string'){");
            template.Append("               @{");
            template.Append("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
            template.Append("                {");
            template.Append("                 <text>");
            template.Append("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
            template.Append("                 </text>");
            template.Append("                }");
            template.Append("                else");
            template.Append("                {");
            template.Append("                 <text>");
            template.Append("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("                 </text>");
            template.Append("                }");
            template.Append("               }");
            template.Append("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
            template.Append("               let timerInterval;");
            template.Append("               Swal.fire({");
            template.Append("                title: 'Auto close alert!',");
            template.Append("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
            template.Append("                timer: 2000,");
            template.Append("                timerProgressBar: true,");
            template.Append("                didOpen: () => {");
            template.Append("                 Swal.showLoading()");
            template.Append("                 const b = Swal.getHtmlContainer().querySelector('b')");
            template.Append("                 timerInterval = setInterval(() => {");
            template.Append("                 b.textContent = Swal.getTimerLeft()");
            template.Append("                }, 100)");
            template.Append("               },");
            template.Append("               willClose: () => {");
            template.Append("                clearInterval(timerInterval)");
            template.Append("               }");
            template.Append("             }).then((result) => {");
            template.Append("               if (result.dismiss === Swal.DismissReason.timer) {");
            template.Append("                console.log('session out .. ');");
            template.Append("                location.href = \"/\";");
            template.Append("               }");
            template.Append("             });");
            template.Append("            } else {");
            template.Append("             location.href = \"/\";");
            template.Append("            }");
            template.Append("           } else {");
            template.Append("            location.href = \"/\";");
            template.Append("           }");
            template.Append("         }).fail(function(xhr)  {");
            template.Append("           console.log(xhr.status)");
            template.Append("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
            template.Append("         }).always(function (){");
            template.Append("          console.log(\"always:complete\");    ");
            template.Append("         });");
            template.Append("       } else if (result.dismiss === swal.DismissReason.cancel) {");
            template.Append("        Swal.fire({");
            template.Append("          icon: 'error',");
            template.Append("          title: 'Cancelled',");
            template.Append("          text: 'Be careful before delete record'");
            template.Append("        })");
            template.Append("       }");
            template.Append("      });");
            template.Append("    }");
            template.Append("    </script>");




            return template.ToString();
        }
        public string GenerateRepository(string tableName, string module)
        {
            var ucTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.UcWords);
            var lcTableName = GetTableNameNoUnderScore(tableName, (int)TextCase.LcWords);
            List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
            List<string?> fieldName = describeTableModels.Select(x => x.FieldValue).ToList();
            var onLineFieldName = String.Join(',', fieldName);
            List<string?> fieldNameParameter = (List<string?>)fieldName.Select(x => "@" + x);
            var onLineFieldParameter = String.Join(',', fieldNameParameter);
            StringBuilder template = new();

            template.Append("using System;");
            template.Append("using System.Collections.Generic;");
            template.Append("using System.IO;");
            template.Append("using ClosedXML.Excel;");
            template.Append("using Microsoft.AspNetCore.Http;");
            template.Append("using MySql.Data.MySqlClient;");
            template.Append("using RebelCmsTemplate.Models."+module+";");
            template.Append("using RebelCmsTemplate.Models.Shared;");
            template.Append("using RebelCmsTemplate.Util;");
            template.Append("namespace RebelCmsTemplate.Repository."+module+";");
            template.Append("    public class "+ucTableName+"Repository");
            template.Append("    {");
            template.Append("        private readonly SharedUtil _sharedUtil;");
            template.Append("        public "+ucTableName+"Repository(IHttpContextAccessor httpContextAccessor)");
            template.Append("        {");
            template.Append("            _sharedUtil = new SharedUtil(httpContextAccessor);");
            template.Append("        }");
            template.Append("        public int Create("+ucTableName+"Model "+lcTableName+"Model)");
            template.Append("        {");
            template.Append("            // okay next we create skeleton for the code");
            template.Append("            var lastInsertKey = 0;");
            template.Append("            string sql = string.Empty;");
            template.Append("            List<ParameterModel> parameterModels = new ();");
            template.Append("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.Append("            try");
            template.Append("            {");
            template.Append("                connection.Open();");
            template.Append("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
            template.Append("                sql += @\"INSERT INTO "+tableName+" ("+fieldName+") VALUES ("+fieldNameParameter+");\";");
            template.Append("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.Append("                parameterModels = new List<ParameterModel>");
            // loop start
            template.Append("                {");
            template.Append("                    new ()");
            template.Append("                    {");
            template.Append("                        Key = \"@tenantName\",");
            template.Append("                        Value = tenantModel.TenantName");
            template.Append("                    },");
            // loop end
            template.Append("                };");
            template.Append("                foreach (ParameterModel parameter in parameterModels)");
            template.Append("                {");
            template.Append("                   mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.Append("                }");
            template.Append("                mySqlCommand.ExecuteNonQuery();");
            template.Append("                mySqlTransaction.Commit();");
            template.Append("                lastInsertKey = (int)mySqlCommand.LastInsertedId;");
            template.Append("                mySqlCommand.Dispose();");
            template.Append("            }");
            template.Append("            catch (MySqlException ex)");
            template.Append("            {");
            template.Append("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.Append("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.Append("                throw new Exception(ex.Message);");
            template.Append("            }");
            template.Append("            return lastInsertKey;");

            template.Append("        }");
            template.Append("        public List<"+ucTableName+"Model> Read()");
            template.Append("        {");
            template.Append("            List<"+ucTableName+"Model> "+lcTableName+"Models = new();");
            template.Append("            string sql = string.Empty;");
            template.Append("            List<ParameterModel> parameterModels = new ();");
            template.Append("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.Append("            try");
            template.Append("            {");
            template.Append("                connection.Open();");
            template.Append("                sql = @\"");
            template.Append("                SELECT      *");
            template.Append("                FROM        "+tableName+" ");
            template.Append("                WHERE       isDelete !=1");
            template.Append("                ORDER BY    "+lcTableName+"Id DESC \";");
            template.Append("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.Append("                using (var reader = mySqlCommand.ExecuteReader())");
            template.Append("                {");
            template.Append("                    while (reader.Read())");
            template.Append("                    {");

            template.Append("                        "+ucTableName+"Models.Add(new "+lcTableName+"Model");
            template.Append("                       {");
            // start loop here
            template.Append("                            TenantName = reader[\"tenantName\"].ToString(),");
            template.Append("                            TenantKey = Convert.ToInt32(reader[\"tenantId\"])");
            template.Append("                        });");
            // end loop here
            template.Append("                    }");
            template.Append("                }");
            template.Append("                mySqlCommand.Dispose();");
            template.Append("            }");
            template.Append("            catch (MySqlException ex)");
            template.Append("            {");
            template.Append("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.Append("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.Append("               throw new Exception(ex.Message);");
            template.Append("            }");

            template.Append("            return "+lcTableName+"Models;");
            template.Append("        }");
            template.Append("        public List<"+ucTableName+"Model> Search(string search)");
            template.Append("       {");
            template.Append("            List<" + ucTableName + "Model> " + lcTableName + "Models = new();");
            template.Append("            string sql = string.Empty;");
            template.Append("            List<ParameterModel> parameterModels = new ();");
            template.Append("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.Append("            try");
            template.Append("            {");
            template.Append("                connection.Open();");
            template.Append("                sql += @\"");
            template.Append("                SELECT  *");
            template.Append("                FROM    "+tableName+" ");
            template.Append("                WHERE   isDelete != 1");
            template.Append("                AND     tenantName like concat('%',@search,'%'); \";");
            template.Append("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.Append("                parameterModels = new List<ParameterModel>");
            template.Append("                {");
            template.Append("                    new ()");
            template.Append("                    {");
            template.Append("                        Key = \"@search\",");
            template.Append("                        Value = search");
            template.Append("                    }");
            template.Append("                };");
            template.Append("                foreach (ParameterModel parameter in parameterModels)");
            template.Append("                {");
            template.Append("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.Append("                }");
            template.Append("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
            template.Append("                using (var reader = mySqlCommand.ExecuteReader())");
            template.Append("                {");
            template.Append("                    while (reader.Read())");
            template.Append("                   {");
            template.Append("                         " + ucTableName + "Models.Add(new " + lcTableName + "Model");
            // loop start
            template.Append("                        {");
            template.Append("                            TenantName = reader[\"tenantName\"].ToString(),");
            template.Append("                            TenantKey = Convert.ToInt32(reader[\"tenantId\"])");
            template.Append("                       });");
            // loop end
            template.Append("                    }");
            template.Append("                }");
            template.Append("                mySqlCommand.Dispose();");
            template.Append("            }");
            template.Append("            catch (MySqlException ex)");
            template.Append("            {");
            template.Append("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.Append("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.Append("                throw new Exception(ex.Message);");
            template.Append("            }");

            template.Append("            return tenantModels;");
            template.Append("        }");
            template.Append("        public byte[] GetExcel()");
            template.Append("        {");
            template.Append("            using var workbook = new XLWorkbook();");
            template.Append("            var worksheet = workbook.Worksheets.Add(\"Administrator > "+ucTableName+" \");");
            // loop here
            template.Append("            worksheet.Cell(1, 1).Value = \"No\";");
            // loop end
            template.Append("            worksheet.Cell(1, 2).Value = \"Tenant\";");
            template.Append("            var sql = _sharedUtil.GetSqlSession();");
            template.Append("           var parameterModels = _sharedUtil.GetListSqlParameter();");
            template.Append("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.Append("            try");
            template.Append("            {");
            template.Append("               connection.Open();");
            template.Append("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.Append("                if (parameterModels != null)");
            template.Append("                {");
            template.Append("                    foreach (ParameterModel parameter in parameterModels)");
            template.Append("                    {");
            template.Append("                        mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.Append("                    }");
            template.Append("                }");
            template.Append("                using (var reader = mySqlCommand.ExecuteReader())");
            template.Append("                {");
            template.Append("                    var counter = 1;");
            template.Append("                   while (reader.Read())");
            template.Append("                    {");
            template.Append("                        var currentRow = counter++;");
            // loop here
            template.Append("                        worksheet.Cell(currentRow, 1).Value = counter-1;");
            template.Append("                        worksheet.Cell(currentRow, 2).Value = reader[\"tenantName\"].ToString();");
            // loop end here
            template.Append("                    }");
            template.Append("                }");
            template.Append("                mySqlCommand.Dispose();");
            template.Append("            }");
            template.Append("            catch (MySqlException ex)");
            template.Append("            {");
            template.Append("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.Append("                throw new Exception(ex.Message);");
            template.Append("            }");
            template.Append("            using var stream = new MemoryStream();");
            template.Append("           workbook.SaveAs(stream);");
            template.Append("            return stream.ToArray();");
            template.Append("        }");
            template.Append("        public void Update(" + ucTableName + "Model " + lcTableName + "Model)");
            template.Append("        {");
            template.Append("            string sql = string.Empty;");
            template.Append("            List<ParameterModel> parameterModels = new ();");
            template.Append("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.Append("            try");
            template.Append("            {");
            template.Append("                connection.Open();");
            template.Append("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
            template.Append("                sql = @\"");
            template.Append("                UPDATE  "+tableName+" ");
            // start loop
            template.Append("                SET     tenantName  =   @tenantName");
            // end loop
            template.Append("                WHERE   " + lcTableName + "Id    =   @" + lcTableName + "Id \";");
            template.Append("                MySqlCommand mySqlCommand = new(sql, connection);");
            // loop here
            template.Append("                mySqlCommand.Parameters.AddWithValue(\"@tenantName\", tenantModel.TenantName);");
            template.Append("                mySqlCommand.Parameters.AddWithValue(\"@tenantId\", tenantModel.TenantKey);");
            // loop end
            template.Append("                parameterModels = new List<ParameterModel>");
            template.Append("                {");
            // loop start
            template.Append("                    new ()");
            template.Append("                    {");
            template.Append("                        Key = \"@tenantId\",");
            template.Append("                        Value = _sharedUtil.GetTenantId()");
            template.Append("                    },");
            // loop end
            template.Append("                    new ()");
            template.Append("                    {");
            template.Append("                       Key = \"@tenantName\",");
            template.Append("                        Value = tenantModel.TenantName");
            template.Append("                    },");
            template.Append("                    new ()");
            template.Append("                   {");
            template.Append("                        Key = \"@executeBy\",");
            template.Append("                        Value = _sharedUtil.GetUserName()");
            template.Append("                    }");
            template.Append("               };");
            template.Append("                foreach (ParameterModel parameter in parameterModels)");
            template.Append("                {");
            template.Append("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.Append("                }");
            template.Append("                mySqlCommand.ExecuteNonQuery();");
            template.Append("                mySqlTransaction.Commit();");
            template.Append("                mySqlCommand.Dispose();");
            template.Append("            }");
            template.Append("            catch (MySqlException ex)");
            template.Append("            {");
            template.Append("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.Append("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.Append("                throw new Exception(ex.Message);");
            template.Append("            }");
            template.Append("        }");
            template.Append("        public void Delete(" + ucTableName + "Model " + lcTableName + "Model)");
            template.Append("        {");
            template.Append("            string sql = string.Empty;");
            template.Append("            List<ParameterModel> parameterModels = new ();");
            template.Append("            using MySqlConnection connection = SharedUtil.GetConnection();");
            template.Append("            try");
            template.Append("            {");
            template.Append("                connection.Open();");
            template.Append("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
            template.Append("                sql = @\"");
            template.Append("                UPDATE  "+tableName+" ");
            template.Append("                SET     isDelete    =   1");
            template.Append("                WHERE   " + lcTableName + "Id    =   @tenantId;\";");
            template.Append("                MySqlCommand mySqlCommand = new(sql, connection);");
            template.Append("                mySqlCommand.Parameters.AddWithValue(\"@tenantId\", tenantModel.TenantKey);");
            template.Append("                parameterModels = new List<ParameterModel>");
            template.Append("                {");
            template.Append("                    new ()");
            template.Append("                    {");
            template.Append("                        Key = \"@"+lcTableName+"Id\",");
            template.Append("                        Value = " + lcTableName + "Model." + lcTableName + "Key");
            template.Append("                   }");
            template.Append("                };");
            template.Append("                foreach (ParameterModel parameter in parameterModels)");
            template.Append("                {");
            template.Append("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
            template.Append("                }");
            template.Append("               mySqlCommand.ExecuteNonQuery();");
            template.Append("                mySqlTransaction.Commit();");
            template.Append("                mySqlCommand.Dispose();");
            template.Append("            }");
            template.Append("            catch (MySqlException ex)");
            template.Append("            {");
            template.Append("                System.Diagnostics.Debug.WriteLine(ex.Message);");
            template.Append("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
            template.Append("                throw new Exception(ex.Message);");
            template.Append("            }");
            template.Append("        }");
            template.Append('}');

            return template.ToString(); ;
        }
        public string GenerateDefaultData(string? tableName)
        {
            using MySqlConnection connection = GetConnection();

            List<DatabaseMapping> db = new List<DatabaseMapping>();
            string sql = @"
            SELECT  TABLE_NAME 
            FROM    information_schema.tables 
            WHERE   table_Schema='fish'";
            if (tableName != null)
            {
                if (tableName.Length > 0)
                {
                    sql += " AND table_name = @table_name";
                }
            }
            var command = new MySqlCommand(sql, connection);
            if (tableName != null)
            {
                if (tableName.Length > 0)
                {
                    command.Parameters.Add("@table_name", MySqlDbType.String).Value = tableName;
                }
            }
            try
            {
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        DatabaseMapping dm = new DatabaseMapping();
                        dm.TableName = reader["TABLE_NAME"].ToString();
                        db.Add(dm);
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
            StringBuilder template = new StringBuilder();
            // loop get all table in database 

            // reloop  
            foreach (dynamic x in db)
            {
                // generate filter key based on foreign key 
                string sqlForeignKey = @"
                SELECT  GROUP_CONCAT(column_name) 
                FROM    information_schema.KEY_COLUMN_USAGE 
                WHERE   table_Schema                =   'fish' 
                AND     referenced_table_schema     !=  ''
                AND     table_name                  = '" + x.tableName + "' ";
                var commandFilter = new MySqlCommand(sqlForeignKey, connection);
                string j = string.Empty;
                try
                {
                    j = (string)commandFilter.ExecuteScalar();
                    List<string> foreignKeyField = j.Split(',').ToList();
                    string table = string.Empty;
                    template.Append("public int Get" +  GetTableNameNoUnderScore(x.tableName, 1) + "(MySqlTransaction _transaction=null");
                    if (foreignKeyField.Count > 0)
                    {
                        StringBuilder im = new();
                        foreach (string n in foreignKeyField)
                        {
                            if (n.Length > 0)
                            {
                                im.Append(",List<int> " + n + "=null");
                            }
                        }
                        template.Append(im);
                    }
                    template.Append(")" + Environment.NewLine);
                    template.Append("{" + Environment.NewLine);
                    template.Append("var defaultValue = 0;" + Environment.NewLine);
                    template.Append("var Sql = @\"SELECT  " +  GetTableNameNoUnderScore(x.tableName, 0) + "Id " + Environment.NewLine);
                    template.Append("FROM " + x.tableName + " " + Environment.NewLine);
                    template.Append("WHERE " + x.tableName + ".isActive = 1 " + Environment.NewLine);
                    template.Append("AND " + x.tableName + ".isDefault = 1\"; " + Environment.NewLine);
                    template.Append("var command = new MySqlCommand();" + Environment.NewLine);
                    template.Append("command.Connection = _connection;" + Environment.NewLine);
                    template.Append("command.Transaction = _transaction;" + Environment.NewLine);
                    if (foreignKeyField.Count > 0)
                    {
                        StringBuilder ik = new();
                        foreach (string n in foreignKeyField)
                        {
                            if (n.Length > 0)
                            {
                                string c = n;
                                string o = n.Replace("Id", "");
                                string k = SplitToUnderScore(o);

                                ik.Append("if (" + n + " != null)" + Environment.NewLine);
                                ik.Append("{" + Environment.NewLine);
                                ik.Append("if (" + n + ".Count > 0)" + Environment.NewLine);
                                ik.Append("{" + Environment.NewLine);
                                ik.Append("var sqlInside = string.Empty;" + Environment.NewLine);
                                ik.Append("Sql = Sql + \" AND    " + x.tableName + "." + c + " IN(\";" + Environment.NewLine);
                                ik.Append("for (var i = 0; i < " + n + ".Count; i++)" + Environment.NewLine);
                                ik.Append("{" + Environment.NewLine);
                                ik.Append("sqlInside = \"@tag\" + i + \", \";" + Environment.NewLine);
                                ik.Append("command.Parameters.AddWithValue(\"@tag\" + i, " + n + ");" + Environment.NewLine);
                                ik.Append("}");
                                ik.Append("Sql = Sql + sqlInside.TrimEnd(',')+ \")\";" + Environment.NewLine);
                                ik.Append("}" + Environment.NewLine);
                                ik.Append("}" + Environment.NewLine);
                                //ik.Append("Sql = Sql + \" ORDER BY " + o + "Description \"; " + Environment.NewLine);
                            }
                        }
                        template.Append(ik);
                    }
                    template.Append("Sql = Sql + \" LIMIT 1 \"; " + Environment.NewLine);
                    template.Append("command.CommandText = Sql;" + Environment.NewLine);

                    template.Append("try" + Environment.NewLine);
                    template.Append("{" + Environment.NewLine);
                    template.Append("defaultValue = Convert.ToInt32(command.ExecuteScalar());" + Environment.NewLine);
                    template.Append("}" + Environment.NewLine);
                    template.Append("catch (MySqlException ex)" + Environment.NewLine);
                    template.Append("{" + Environment.NewLine);
                    template.Append("_global.ErrorLogging(ex.Message, Sql);" + Environment.NewLine);
                    template.Append("}" + Environment.NewLine);
                    template.Append("finally" + Environment.NewLine);
                    template.Append("{" + Environment.NewLine);
                    template.Append("command.Dispose();" + Environment.NewLine);
                    template.Append("}" + Environment.NewLine);
                    template.Append("return defaultValue;" + Environment.NewLine);
                    template.Append("}" + Environment.NewLine);
                    foreignKeyField.Clear();
                }
                catch (MySqlException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                finally
                {
                    commandFilter.Dispose();
                }


            }
            return template.ToString();
        }
        private static string UpperCaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
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
        private static string GetTableNameNoUnderScore(string t, int type)
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
