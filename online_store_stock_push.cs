using System;
using System.Net;
using System.IO;
using System.Diagnostics;

class Program
{

    static void Main()
    {
        Console.WriteLine("----- SIFARMA <-> ZONPHARMA -----\n");

        //Detalhes da ligação ao servidor FTP
        string ftpServer = "ftp://www.zonpharma.com";
        string username = "ftp_zonpharma";
        string password = "password";
        string remoteFilePath = "/import/documents_prods/farmacia.txt";
        string localDirectory = @"C:\inetpub\wwwroot\importarStocksSifarma";
        string phpScriptPath = @"C:\inetpub\wwwroot\importarStocksSifarma\importar.php";
        // Caminho do arquivo de log
        string caminhoArquivoLog = @"C:\inetpub\wwwroot\importarStocksSifarma\logs_script\log.txt";

        // Mensagem para o log
        string mensagem = "";

        //Liga ao servidor FTP
        if (ConnectToFtpServer(ftpServer, username, password))
        {
            mensagem = "Ligado com sucesso ao servidor FTP.";
            // Adiciona a mensagem ao arquivo de log
            RegistrarLog(caminhoArquivoLog, mensagem);

            //Verifica se o ficheiro existe
            if (FileExistsOnFtp(ftpServer, username, password, remoteFilePath))
            {
                mensagem = "Download do ficheiro efetuado com sucesso.";
                // Adiciona a mensagem ao arquivo de log
                RegistrarLog(caminhoArquivoLog, mensagem);

                //Faz download do ficheiro
                DownloadFileFromFtp(ftpServer, username, password, remoteFilePath, localDirectory);
                int milliseconds = 10000;
                Thread.Sleep(milliseconds);

                //Corre o script PHP para importar os produtos
                if (RunPhpScript(phpScriptPath, out string output))
                {
                    // Verificar a string específica na saída do script para saber se importou com sucesso o stock dos produtos
                    if (output.Contains("Pedido efetuado com Sucesso!"))
                    {
                        Console.WriteLine("Script PHP executado com sucesso.\nStock dos produtos da farmácia importado com sucesso.");
                        mensagem = "Script PHP executado com sucesso. Stock dos produtos da farmácia importado com sucesso.";
                        // Adiciona a mensagem ao arquivo de log
                        RegistrarLog(caminhoArquivoLog, mensagem);
                    }
                    
                     else if (output.Contains("Sem artigos a alterar."))
                    {
                        Console.WriteLine("Script PHP executado com sucesso.\nSem artigos a alterar.");
                        mensagem = "Script PHP executado com sucesso. Sem artigos a alterar.";
                        // Adiciona a mensagem ao arquivo de log
                        RegistrarLog(caminhoArquivoLog, mensagem);
                    }
                    else{
                        Console.WriteLine("O script PHP foi executado com sucesso mas ocorreu um erro na importação do stock dos produtos da farmácia.");
                        Console.WriteLine(output);
                        mensagem = "O script PHP foi executado com sucesso mas ocorreu um erro na importação do stock dos produtos da farmácia." + output;
                        // Adiciona a mensagem ao arquivo de log
                        RegistrarLog(caminhoArquivoLog, mensagem);
                    }
                }
                else
                {
                    Console.WriteLine("Erro ao executar o script PHP. Stock não importado.");
                    mensagem = "Erro ao executar o script PHP. Stock não importado.";
                    // Adiciona a mensagem ao arquivo de log
                    RegistrarLog(caminhoArquivoLog, mensagem);
                }
            }
            else
            {
                Console.WriteLine($"O ficheiro {remoteFilePath} não existe no servidor.");
                mensagem = $"O ficheiro {remoteFilePath} não existe no servidor.";
                // Adiciona a mensagem ao arquivo de log
                RegistrarLog(caminhoArquivoLog, mensagem);
            }
        }
        else
        {
            Console.WriteLine("Falha a ligar ao servidor FTP.");
            mensagem = "Falha a ligar ao servidor FTP. Stock não importado.";
            // Adiciona a mensagem ao arquivo de log
            RegistrarLog(caminhoArquivoLog, mensagem);
        }
        Console.WriteLine("Fim do script.");
       
    }

    static void RegistrarLog(string caminhoArquivo, string mensagem)
    {
        // Adiciona a data e hora à mensagem
        string mensagemCompleta = $"{DateTime.Now} - {mensagem}";

        // Escreve a mensagem no arquivo de log
        using (StreamWriter writer = File.AppendText(caminhoArquivo))
        {
            writer.WriteLine(mensagemCompleta);
        }

        Console.WriteLine("Log registrado com sucesso!");
    }

    static bool ConnectToFtpServer(string ftpServer, string username, string password)
    {
        try
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(ftpServer);
            ftpRequest.Credentials = new NetworkCredential(username, password);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;


            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();

            Console.WriteLine($"Ligado com sucesso a {ftpServer}");
            //Console.WriteLine($"Estado: {response.StatusDescription}");

            return true;
            
        }
        catch (WebException ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return false;
        }
    }

    static bool FileExistsOnFtp(string ftpServer, string username, string password, string remoteFilePath)
    {
        try
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create($"{ftpServer}/import/documents_prods/");
            ftpRequest.Credentials = new NetworkCredential(username, password);
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;

            using (FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    Console.WriteLine("O ficheiro foi encontrado.");
                    string fileList = reader.ReadToEnd();
                    return true;
                }
            }
        }
        catch (WebException ex)
        {
            Console.WriteLine($"Erro: {ex.Message}");
            return false;
        }
    }


    static void DownloadFileFromFtp(string ftpServer, string username, string password, string remoteFilePath, string localDirectory)
    {
        using (WebClient ftpClient = new WebClient())
        {
            ftpClient.Credentials = new NetworkCredential(username, password);
            ftpClient.DownloadFile(new Uri(ftpServer + remoteFilePath), Path.Combine(localDirectory, Path.GetFileName(remoteFilePath)));
            Console.WriteLine($"O ficheiro foi transferido para {localDirectory}");
        }
    }

    static bool RunPhpScript(string scriptPath, out string output)
    {
        output = string.Empty;

        // Verificar se o script PHP existe
        if (!System.IO.File.Exists(scriptPath))
        {
            Console.WriteLine($"Erro: O script PHP '{scriptPath}' não foi encontrado.");
            return false;
        }

        // Caminho para o executável do PHP (certifique-se de que o PHP está instalado no sistema)
        
        // meu pc-
        //string phpExePath = @"C:\inetpub\wwwroot\php\php.exe";

        //pc loja
        string phpExePath = @"C:\inetpub\php\php.exe";

        // Argumentos para passar para o script PHP
        string phpArguments = $"-f \"{scriptPath}\"";

        // Configurar o processo de inicialização
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = phpExePath,
            Arguments = phpArguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            // Iniciar o processo
            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();

                // Ler a saída padrão e erro
                output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Aguardar até que o processo seja concluído
                process.WaitForExit();

                // Verificar se ocorreu um erro durante a execução
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Erro durante a execução do script PHP: {error}");
                    return false;
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao executar o script PHP: {ex.Message}");
            return false;
        }
    }

}
