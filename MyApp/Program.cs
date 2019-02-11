using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using Google.Apis.Auth.OAuth2;
using MimeTypes;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using Google.Apis.Services;

namespace MyApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var credenciais = Autenticar();

            using (var servico = AbrirServico(credenciais))
            {
                //Listar Arquivos
                //Console.WriteLine("Listagem");
                //ListarArquivos(servico, 10);
                //Console.WriteLine("Fim Listagem");
                //Console.ReadLine();

                //Criar Diretorio
                //Console.Clear();
                //Console.WriteLine("Criar Diretorio");
                //CriarDiretorio(servico, "Novo Diretorio");
                //Console.WriteLine("Fim Criar Diretorio");
                //Console.ReadLine();

                //Excluir Diretorio
                //Console.Clear();
                //Console.WriteLine("Deletar Item");
                //DeletarItem(servico, "Novo Diretorio");
                //Console.WriteLine("Fim Deletar Item");
                //Console.ReadLine();

                //Upload de Arquivo
                //Console.Clear();
                //Console.WriteLine("Upload");
                //Upload(servico, "arquivo.txt");
                //Console.WriteLine("Fim Upload");
                //Console.ReadLine();

                //Download de Arquivo
                //Console.Clear();
                //Console.WriteLine("Download");
                //Download(servico, "torneio naruto.xlsx", "TorneioNarutoBaixado.xlsx");
                //Console.WriteLine("Fim Download");
                //Console.ReadLine();

                //Mover Arquivo para a Lixeira
                //Console.Clear();
                //Console.WriteLine("Movendo para a Lixeira");
                //MoverParaLixeira(servico, "arquivo.txt");
                //Console.WriteLine("Fim Lixeira");
                //Console.ReadLine();

                //Tutorial at http://www.andrealveslima.com.br/blog/index.php/2017/04/12/utilizando-api-google-drive-no-c-e-vb-net/
            }
        }
        
        private static UserCredential Autenticar()
        {
            UserCredential credenciais;
            using(var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                var diretorioAtual = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var diretorioCredenciais = Path.Combine(diretorioAtual, "credential");

                credenciais = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { DriveService.Scope.Drive },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(diretorioCredenciais, true)).Result;
            }

            return credenciais;
        }

        private static DriveService AbrirServico(UserCredential credenciais)
        {
            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credenciais
            });
        }

        private static void ListarArquivos(DriveService servico, int arquivosPorPagina)
        {
            var request = servico.Files.List();
            request.Fields = "nextPageToken, files(id, name)";
            request.PageSize = arquivosPorPagina;
            request.Q = "trashed=false";
            var resultado = request.Execute();
            var arquivos = resultado.Files;

            while(arquivos != null && arquivos.Any())
            {
                foreach(var arquivo in arquivos)
                {
                    Console.WriteLine(arquivo.Name);
                }

                if(resultado.NextPageToken != null)
                {
                    Console.WriteLine("Digite ENTER para ir para a proxima pagina");
                    Console.ReadLine();
                    request.PageToken = resultado.NextPageToken;
                    resultado = request.Execute();
                    arquivos = resultado.Files;
                }
                else
                {
                    arquivos = null;
                }
            }
        }

        private static void CriarDiretorio(DriveService servico, string nomeDiretorio)
        {
            var diretorio = new Google.Apis.Drive.v3.Data.File();
            diretorio.Name = nomeDiretorio;
            diretorio.MimeType = "application/vnd.google-apps.folder";
            var request = servico.Files.Create(diretorio);
            request.Execute();
        }

        private static string[] ProcurarArquivoId(DriveService servico, string nome, bool procurarNaLixeira)
        {
            var retorno = new List<string>();
            var request = servico.Files.List();
            request.Q = string.Format("name = '{0}'", nome);
            if (!procurarNaLixeira)
            {
                request.Q += " and trashed = false";
            }

            request.Fields = "files(id)";
            var resultado = request.Execute();
            var arquivos = resultado.Files;

            if (arquivos != null && arquivos.Any())
            {
                foreach (var arquivo in arquivos)
                {
                    retorno.Add(arquivo.Id);
                }
            }

            return retorno.ToArray();
        }

        private static void DeletarItem(DriveService servico, string nome)
        {
            var ids = ProcurarArquivoId(servico, nome, false);
            if (ids != null && ids.Any())
            {
                foreach (var id in ids)
                {
                    var request = servico.Files.Delete(id);
                    request.Execute();
                }
            }
        }

        private static void Upload(DriveService servico, string caminhoArquivo)
        {
            var arquivo = new Google.Apis.Drive.v3.Data.File();
            arquivo.Name = Path.GetFileName(caminhoArquivo);
            arquivo.MimeType = MimeTypeMap.GetMimeType(Path.GetExtension(caminhoArquivo));
            using (var stream = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read))
            {
                var request = servico.Files.Create(arquivo, stream, arquivo.MimeType);
                request.Upload();
            };
        }

        private static void Download(DriveService servico, string nome, string destino)
        {
            var ids = ProcurarArquivoId(servico, nome, false);
            if (ids != null && ids.Any())
            {
                var request = servico.Files.Get(ids.First());
                using (var stream = new FileStream(destino, FileMode.Create, FileAccess.Write))
                {
                    request.Download(stream);
                }
            }
        }

        private static void MoverParaLixeira(DriveService servico, string nome)
        {
            var ids = ProcurarArquivoId(servico, nome, false);

            if (ids != null && ids.Any())
            {
                foreach (var id in ids)
                {
                    var arquivo = new Google.Apis.Drive.v3.Data.File();
                    arquivo.Trashed = true;
                    var request = servico.Files.Update(arquivo, id);
                    request.Execute();
                }
            }
        }
    }
}
