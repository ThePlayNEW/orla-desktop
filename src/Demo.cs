using System;
using System.IO;
using System.Linq;

namespace Orla
{
    public static class Demo
    {
        public static Layout create()
        {
            var layout = new Layout();
            string sampleDirectory = Path.Combine(Path.GetTempPath(), "Orla-preview");
            Directory.CreateDirectory(sampleDirectory);
            string[] names = { "Projetos", "Aplicativos", "Arquivos", "Acesso rápido" };
            string[] colors = { "#91DFC4", "#8BBEFF", "#B6A3F5", "#E9C58A" };
            string[][] labels = { new[] { "Website", "Identidade visual", "Protótipos", "Fotografia",
                                          "Portfólio", "Referências" },
                                  new[] { "Navegador", "Editor", "Terminal", "Música", "Calendário",
                                          "Notas" },
                                  new[] { "Documentos", "Imagens", "Downloads", "Apresentações" },
                                  new[] { "Este Computador", "Lixeira", "Bibliotecas" } };
            for (int index = 0; index < names.Length; index++)
            {
                var group = new Group { Name = names[index], Color = colors[index], Visible = true };
                foreach (string label in labels[index])
                    group.Items.Add(new Entry { Name = label, Path = sampleDirectory });
                layout.Groups.Add(group);
            }
            Discovery.position(layout);
            return layout;
        }
    }
}
