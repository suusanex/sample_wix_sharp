using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using WixSharp;
using WixSharp.Bootstrapper;
using WixSharp.UI.WPF;

namespace WixSharpV4ManagedWPFUI
{
    internal class Program
    {
        static void Main()
        {
            var project = new ManagedProject("MyProduct",
                              new Dir(@"%ProgramFiles%\My Company\My Product",
                                  new File("Program.cs")));

            project.GUID = new Guid("3d7039c1-8281-4b71-97fe-819ede6757da");

            // project.ManagedUI = ManagedUI.DefaultWpf; // all stock UI dialogs

            //custom set of UI WPF dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI.InstallDialogs.Add<WixSharpV4ManagedWPFUI.WelcomeDialog>()
                                            .Add<WixSharpV4ManagedWPFUI.LicenceDialog>()
                                            .Add<WixSharpV4ManagedWPFUI.FeaturesDialog>()
                                            .Add<WixSharpV4ManagedWPFUI.InstallDirDialog>()
                                            .Add<WixSharpV4ManagedWPFUI.ProgressDialog>()
                                            .Add<WixSharpV4ManagedWPFUI.ExitDialog>();

            project.ManagedUI.ModifyDialogs.Add<WixSharpV4ManagedWPFUI.MaintenanceTypeDialog>()
                                           .Add<WixSharpV4ManagedWPFUI.FeaturesDialog>()
                                           .Add<WixSharpV4ManagedWPFUI.ProgressDialog>()
                                           .Add<WixSharpV4ManagedWPFUI.ExitDialog>();

            //project.SourceBaseDir = "<input dir path>";
            //project.OutDir = "<output dir path>";

            var productMsi = project.BuildMsi();


            var bootstrapper =
                new Bundle("MyProduct",
                    AddVCRuntimeX64(),
                    new MsiPackage(productMsi) { DisplayInternalUI = true }
                );

            bootstrapper.Application = new WixInternalUIBootstrapperApplication();

            bootstrapper.Include(WixExtension.Util);

            bootstrapper.Variables = new[]
            {
                new Variable(VariableName_Vcredistx64BundleVersion, VCRuntimeX64BundleVersion, VariableType.version),
            };

            bootstrapper.AddWixFragment("Wix/Bundle",
                new UtilRegistrySearch
                {
                    Root = RegistryHive.LocalMachine,
                    Key = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\X64",
                    Value = "Version",
                    Variable = VariableName_Vcredistx64InstalledVersion,
                });


            bootstrapper.Version = new Version("1.0.0.0");
            bootstrapper.UpgradeCode = new Guid("015b4a2a-1fe3-407c-a01c-dcab214c6faf");
            // bootstrapper.PreserveTempFiles = true;

            bootstrapper.WixSourceGenerated += WixSourceConvert;

            bootstrapper.Build("MyProduct.exe");

        }

        /// <summary>
        /// wixsharp未対応のWiX4の記法について、ここで変換する。
        /// </summary>
        /// <param name="document"></param>
        private static void WixSourceConvert(XDocument document)
        {
            document.FindAll("RemotePayload").ForEach(remotePayload =>
            {
                var exePackagePayload = new XElement("ExePackagePayload");
                exePackagePayload.Add(remotePayload.Attributes());

                if (remotePayload.Parent.HasAttribute("DownloadUrl"))
                {
                    var attribute = remotePayload.Parent.Attribute("DownloadUrl");
                    attribute.Remove();
                    exePackagePayload.Add(attribute);
                }

                if (remotePayload.Parent.HasAttribute("Name"))
                {
                    var attribute = remotePayload.Parent.Attribute("Name");
                    attribute.Remove();
                    exePackagePayload.Add(attribute);
                }

                remotePayload.AddAfterSelf(exePackagePayload);
                remotePayload.Remove();
            });

        }

        private static readonly string VariableName_Vcredistx64InstalledVersion = "VCRedistX64InstalledVer";
        private static readonly string VariableName_Vcredistx64BundleVersion = "VCRedistX64BundleVer";
        private static readonly string VCRuntimeX64BundleVersion = "14.38.33130.0";

        private static ExePackage AddVCRuntimeX64()
        {
            var inst = new ExePackage
            {
                Id = "VCRuntimeX64",
                Name = "VC_redist.x64.exe",
                Vital = true,
                Permanent = true,
                DownloadUrl = @"https://download.visualstudio.microsoft.com/download/pr/a061be25-c14a-489a-8c7c-bb72adfb3cab/4DFE83C91124CD542F4222FE2C396CABEAC617BB6F59BDCBDF89FD6F0DF0A32F/VC_redist.x64.exe",
                InstallArguments = "/install /quiet /norestart",
                RepairArguments = "/repair /quiet /norestart",
                UninstallArguments = "/uninstall /quiet /norestart",
                LogPathVariable = "VCRuntimeX64.log",
                DetectCondition = $"{VariableName_Vcredistx64InstalledVersion} >= {VariableName_Vcredistx64BundleVersion}",
                RemotePayloads = new[]
                {
                    new RemotePayload
                    {
                        ProductName = "Microsoft Visual C++ 2015-2022 Redistributable (x64) - 14.38.33130",
                        Description = "Microsoft Visual C++ 2015-2022 Redistributable (x64) - 14.38.33130",
                        CertificatePublicKey = "3C7BE5E85FC8FA0E53392DC373230208C9CBB8AB",
                        CertificateThumbprint = "7E9572FFDB0BE9E618862EB6463B2C0782FC2DB9",
                        Size = 25424536,
                        Version = VCRuntimeX64BundleVersion.ToRawVersion(),
                    }
                }
            };
            inst.Attributes.Add("CacheId", "VCRuntimeX64Cache");
            return inst;
        }
    }
}