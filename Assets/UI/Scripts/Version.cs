using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ConsoleUtility;

public class Version : MonoBehaviour
{
    public TMP_Text versionField;

    // Start is called before the first frame update
    void Start()
    {
        var versionInfo = Application.version;

        // @note: System.Diagnostics.FileVersionInfo.GetVersionInfo() is not supported with Unity's
        //         IL2CPP, so we are out of luck for displaying the file product version read from the exe.
        //var exeFiles = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), "Spaceship Demo.exe");
        //if (exeFiles != null && exeFiles.Length > 0)
        //{
        //    var exePath = exeFiles[0];
        //    if (!string.IsNullOrEmpty(exePath))
        //    {
        //        Console.Log(string.Format("Executable path: {0}", exePath));
        //        var fileInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
        //        versionInfo = fileInfo.ProductVersion;
        //    }
        //}

        var versionText = string.Format("Version: {0}\nUnity: {1}",
            versionInfo, Application.unityVersion);

        versionField.text = versionText;
    }
}
