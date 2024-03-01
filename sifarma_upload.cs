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
        string remoteFilePathUpload = "/import/documents_prods/";
        string localDirectory = @"C:\EXPORTDATA\PRODUTOS";
        string phpScriptPath = @"C:\inetpub\wwwroot\importarStocksSifarma\importar.php";
        string localFilePath = @"C:\EXPORTDATA\PRODUTOS\farmacia.txt";

        // Caminho do arquivo de log
        string caminhoArquivoLog = @"C:\EXPORTDATA\PRODUTOS\log_uploads\log.txt";

        //paths necessários para verificar se já tem um ficheiro novo infoprex
        string pastaMonitorada = @"C:\EXPORTDATA\PRODUTOS";
        string executavelPath = @"C:\Sif2000\prog\ExportaInfoprex.exe";

        // Mensagem para o log
        string mensagem = "";

        // Executa o executável
        ExecutarExe(executavelPath);

        
        //Liga ao servidor FTP
        if (ConnectToFtpServer(ftpServer, username, password))
        {
            Console.WriteLine("Ligado com sucesso ao servidor FTP.");

            mensagem = "Ligado com sucesso ao servidor FTP.";
            // Adiciona a mensagem ao arquivo de log
            RegistrarLog(caminhoArquivoLog, mensagem);

            
            //Verifica se o ficheiro existe
            if (FileExistsOnFtp(ftpServer, username, password, remoteFilePath))
            {

                //elimina o ficheiro para substituir por um mais recente
                ExcluirArquivoFtp(ftpServer, username, password, "/import/documents_prods/farmacia.txt");

                Console.WriteLine("Ficheiro mais antigo eliminado no servidor FTP.");

                // Adiciona a mensagem ao arquivo de log
                mensagem = "Ficheiro mais antigo eliminado no servidor FTP.";
                RegistrarLog(caminhoArquivoLog, mensagem);

                //faz upload do ficheiro
                UploadFileToFtp(ftpServer, username, password, localFilePath, remoteFilePathUpload);

                Console.WriteLine("Upload efectuado com sucesso.");

                // Adiciona a mensagem ao arquivo de log
                mensagem = "Upload efectuado com sucesso.";
                RegistrarLog(caminhoArquivoLog, mensagem);
            }
            else
            {
                //faz upload do ficheiro
               UploadFileToFtp(ftpServer, username, password, localFilePath, remoteFilePathUpload);

                Console.WriteLine("Upload efectuado com sucesso.");

                mensagem = "Upload efectuado com sucesso.";
                RegistrarLog(caminhoArquivoLog, mensagem);
            }
            
        }
        else
        {
            Console.WriteLine("Falha a ligar ao servidor FTP.");

            mensagem = "Falha a ligar ao servidor FTP. Nao foi efetuado o upload..";
            // Adiciona a mensagem ao arquivo de log
            RegistrarLog(caminhoArquivoLog, mensagem);
        }
        Console.WriteLine("Fim do script.");

        // Adiciona a mensagem ao arquivo de log
        mensagem = "Fim do script.";
        RegistrarLog(caminhoArquivoLog, mensagem);

    }

    //faz upload do ficheiro para a redicom
    static void UploadFileToFtp(string ftpServer, string username, string password, string localFilePath, string remoteDirectory)
    {
        try
        {
            // Cria a solicitação FTP
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{ftpServer}{remoteDirectory}farmacia.txt");
            request.Credentials = new NetworkCredential(username, password);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // Lê o arquivo local
            byte[] fileContents = File.ReadAllBytes(localFilePath);

            // Envia o arquivo para o servidor FTP
            request.ContentLength = fileContents.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(fileContents, 0, fileContents.Length);
            }


            // Obtém a resposta do servidor
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Arquivo {remoteDirectory}farmacia.txt enviado com sucesso. Status: {response.StatusDescription}");
            }
        }
        catch (WebException ex)
        {
            Console.WriteLine($"Erro ao enviar o arquivo para o servidor FTP: {ex.Message}");
        }
    }

    //renomeia o ficheiro exportado para farmacia.txt
    static void RenomearArquivoAposConclusao(string pastaMonitorada)
    {
        try
        {
            // Verifica se existe o arquivo "farmacia.txt" na pasta
            string caminhoFarmacia = Path.Combine(pastaMonitorada, "farmacia.txt");
            if (File.Exists(caminhoFarmacia))
            {
                // Se existir, exclui o arquivo "farmacia.txt"
                File.Delete(caminhoFarmacia);
                Console.WriteLine($"Ficheiro antigo {caminhoFarmacia} eliminado.");
            }

            // Obtém todos os arquivos .txt na pasta
            string[] arquivos = Directory.GetFiles(pastaMonitorada, "*.txt");

            // Verifica se existem arquivos .txt na pasta
            if (arquivos.Length > 0)
            {
                // Obtém o arquivo .txt mais recente
                string arquivoMaisRecente = arquivos.OrderByDescending(f => new FileInfo(f).CreationTime).First();

                // Gera o novo nome para o arquivo
                string novoNomeArquivo = "farmacia.txt";

                // Cria o caminho completo para o novo arquivo
                string novoCaminhoArquivo = Path.Combine(pastaMonitorada, novoNomeArquivo);

                // Renomeia o arquivo
                File.Move(arquivoMaisRecente, novoCaminhoArquivo);

                Console.WriteLine($"Ficheiro renomeado para {novoCaminhoArquivo}.");
            }
            else
            {
                Console.WriteLine("Nenhum ficheiro .txt encontrado para renomear na pasta.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao renomear o ficheiro: {ex.Message}");
        }
    }


    //corre o executavel que exporta os dados do sifarma
    static void ExecutarExe(string exePath)
    {
        string localDirectory = @"C:\EXPORTDATA\PRODUTOS";

        // Inicia o processo do executável em uma janela separada
        try
        {
            Console.WriteLine("A exportar os dados...Aguarde.");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exePath,
                CreateNoWindow = false,  // Configura para criar uma nova janela
                UseShellExecute = true  // Usa o shell para iniciar o processo
            };

            using (Process processo = new Process { StartInfo = psi })
            {
                processo.Start();
                processo.WaitForExit();

                // Verifica se o processo terminou
                if (processo.HasExited)
                {
                    Console.WriteLine($"O executável {exePath} terminou.\n");

                    // Adiciona a lógica para renomear o arquivo após a conclusão do processo
                    RenomearArquivoAposConclusao(localDirectory);
                    Console.WriteLine("Arquivo renomeado.\n");
                   
                    // Adiciona a mensagem ao arquivo de log
                    string mensagem = "Ficheiro exportado e renomeado para farmacia.txt com sucesso.";
                    string caminhoArquivoLog = @"C:\EXPORTDATA\PRODUTOS\log_uploads\log.txt";
                    RegistrarLog(caminhoArquivoLog, mensagem);
                }
            }

            Console.WriteLine("Ficheiro exportado com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao executar o executável: {ex.Message}");
            // Adiciona a mensagem ao arquivo de log
            string mensagem = $"Ocorreu um erro a exportar o ficheiro e/ou a renomear:{ex.Message}";
            string caminhoArquivoLog = @"C:\EXPORTDATA\PRODUTOS\log_uploads\log.txt";
            RegistrarLog(caminhoArquivoLog, mensagem);
        }
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

    //apaga o ficheiro se ele existir, para colocar um mais recente
    static void ExcluirArquivoFtp(string ftpServer, string username, string password, string remoteFilePath)
    {
        try
        {
            // Cria a solicitação FTP
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{ftpServer}{remoteFilePath}");
            request.Credentials = new NetworkCredential(username, password);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            // Executa a solicitação
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Console.WriteLine($"Arquivo {remoteFilePath} excluído com sucesso. Status: {response.StatusDescription}");
            }
        }
        catch (WebException ex)
        {
            Console.WriteLine($"Erro ao excluir o arquivo {remoteFilePath}: {ex.Message}");
        }
    }

    //faz download do ficheiro
    static void DownloadFileFromFtp(string ftpServer, string username, string password, string remoteFilePath, string localDirectory)
    {
        using (WebClient ftpClient = new WebClient())
        {
            ftpClient.Credentials = new NetworkCredential(username, password);
            ftpClient.DownloadFile(new Uri(ftpServer + remoteFilePath), Path.Combine(localDirectory, Path.GetFileName(remoteFilePath)));
            Console.WriteLine($"O ficheiro foi transferido para {localDirectory}");
        }
    }

   

}

