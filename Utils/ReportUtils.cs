using AspNetCore.Reporting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using System.Text;

namespace proyecto_backend.Utils
{
    public static class ReportUtils
    {
        public static LocalReport GetReport(string reportName)
        {
            string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("proyecto_backend.dll", string.Empty);
            string projectDir = Directory.GetParent(fileDirPath).Parent.Parent.Parent.FullName;

            string rdlcFilePath = string.Format("{0}\\Reports\\{1}.rdlc", projectDir, reportName);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("utf-8");

            LocalReport report = new(rdlcFilePath);

            return report;
        }

        public static string getPath(string reportName)
        {
            string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("proyecto_backend.dll", string.Empty);
            string projectDir = Directory.GetParent(fileDirPath).Parent.Parent.Parent.FullName;


            string rdlcFilePath = string.Format("{0}\\Reports\\{1}.rdlc", projectDir, reportName);
            return rdlcFilePath;
        }
    }
}