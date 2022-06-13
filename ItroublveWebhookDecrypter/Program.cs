using dnlib.DotNet;
using dnlib.IO;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ItroublveWebhookDecrypter
{
    internal static class Program
    {
        //Don't use "<methodDef>.Body.Instructions[i].Operand.ToString().Contains("something")" go straight for a better verification method, use return type, parameters type & names, additionally verify the content of the method(to check if specific instructions are there)
        private static ModuleDefMD mainModule = null, grabberModule = null;
        private static Assembly asm = null;
        private static string encryptedWebhook = string.Empty, grabberPath = string.Empty;
        static void Main(string[] args)
        {
            mainModule = ModuleDefMD.Load(args[0]);
            MethodDef mainMethod = mainModule.EntryPoint;
            for (int i = 0; i < mainMethod.Body.Instructions.Count(); i++)
            {
                try
                {
                    if (mainMethod.Body.Instructions[i].Operand.ToString().Contains("Process::Start(System.String,System.String)")) encryptedWebhook = mainMethod.Body.Instructions[i - 6].Operand.ToString();
                }
                catch { }
            }

            foreach (Resource res in mainModule.Resources)
            {
                if (res.Name.Contains("RtkBtManServ.exe"))
                {
                    EmbeddedResource myResLmao = mainModule.Resources.FindEmbeddedResource(res.Name);
                    DataReader reader = myResLmao.CreateReader();
                    using (Stream bufferStream = reader.AsStream())
                    {
                        try
                        {
                            grabberPath = Path.Combine($"{Directory.GetCurrentDirectory()}\\", "hello.bin");
                            File.WriteAllBytes(grabberPath, ReadFully(bufferStream));
                            grabberModule = ModuleDefMD.Load(grabberPath);
                            asm = Assembly.LoadFile(grabberPath);
                            MethodDef mainGrabberMethod = grabberModule.EntryPoint;
                            for (int i = 0; i < mainGrabberMethod.Body.Instructions.Count(); i++)
                            {
                                try
                                {
                                    if (mainGrabberMethod.Body.Instructions[i].Operand.ToString().Contains("StealerExt.Hook::AES128(System.Byte[])"))
                                    {
                                        try
                                        {
                                            MethodDef decryptMethod = mainGrabberMethod.Body.Instructions[i].Operand as MethodDef;
                                            byte[] encryptedWebhookFromBase64 = Convert.FromBase64String(encryptedWebhook);
                                            string decryptedWebhook = Encoding.ASCII.GetString((byte[])((MethodInfo)asm.ManifestModule.ResolveMethod((int)decryptMethod.MDToken.Raw)).Invoke(null, new object[] { encryptedWebhookFromBase64 }));
                                            Console.WriteLine($"Decrypted WebHook: {decryptedWebhook}");
                                        }
                                        catch { }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
            }
            Console.ReadLine();
        }

        //https://stackoverflow.com/a/221941 (it's sexier than a CopyTo)
        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    ms.Write(buffer, 0, read);
                return ms.ToArray();
            }
        }
    }
}
//If you want you can improve the code a bit(detections, support obfuscated version etc..) :)