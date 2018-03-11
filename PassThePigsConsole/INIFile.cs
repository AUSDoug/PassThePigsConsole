using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

//This .ini handling code comes courtesy of Smart Arab.
//And, no, I'm not a racist; http://www.smart-arab.com/2013/05/using-ini-file-with-c/
//Apparently, ini files are 'outdated' and we're supposed to use 'application settings' and things.
//What a load of crap; INI files are awesome.
//This guy knows that.

namespace PassThePigsConsole
{
    class INIFile
    {
        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(string ApplicationName, string KeyName, string StrValue, string FileName);
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(string ApplicationName, string KeyName, string DefaultValue, StringBuilder ReturnString, int nSize, string FileName);


        public static void WriteValue(string SectionName, string KeyName, string KeyValue, string FileName)
        {
            WritePrivateProfileString(SectionName, KeyName, KeyValue, FileName);
        }

        public static string ReadValue(string SectionName, string KeyName, string FileName)
        {
            StringBuilder szStr = new StringBuilder(255);
            GetPrivateProfileString(SectionName, KeyName, "", szStr, 255, FileName);
            return szStr.ToString().Trim();
        }
    }
}
