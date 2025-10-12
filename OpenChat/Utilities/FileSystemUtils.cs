using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenChat.Utilities;

internal static class FileSystemUtils
{
    //【重点】获取exe的目录
    public static string GetEntryPointFolder()
    {
        //.dll或者.exe
        var entryPointAsm = Assembly.GetEntryAssembly();
        if (entryPointAsm == null)
            throw new InvalidOperationException("App was started from unmanaged code");
        //从.exe文件提取出目录
        return Path.GetDirectoryName(entryPointAsm.Location) ?? ".";
    }
}