using System;
using System.IO;
using System.Collections.Generic;

namespace Orla
{
    public static class Tests
    {
        static void check(bool ok, string name, List<string> results)
        {
            if (!ok)
                throw new Exception("FAIL: " + name);
            results.Add("PASS: " + name);
        }
        public static void run(string report)
        {
            var results = new List<string>();
            string root = Path.Combine(Path.GetTempPath(), "Orla-unit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string file = Path.Combine(root, "ação com espaço.txt");
            File.WriteAllText(file, "conteudo preservado");
            var s = new Store(Path.Combine(root, "layout.json")) { data = new Layout() };
            var a = new Group { Name = "A" };
            var b = new Group { Name = "B" };
            s.data.Groups.Add(a);
            s.data.Groups.Add(b);
            check(s.add(a, file), "adicionar arquivo com Unicode", results);
            check(!s.add(a, file), "evitar referência duplicada", results);
            check(!s.add(a, Path.Combine(root, "missing")), "rejeitar caminho ausente", results);
            string id = a.Items[0].Id;
            check(s.move(id, b, 999) && a.Items.Count == 0 && b.Items.Count == 1,
                  "mover entre grupos sem mover arquivo", results);
            check(File.ReadAllText(file) == "conteudo preservado", "conteúdo original preservado", results);
            string second = Path.Combine(root, "segundo.txt");
            File.WriteAllText(second, "2");
            s.add(b, second);
            s.move(b.Items[1].Id, b, 0);
            check(b.Items[0].Path == second, "reordenar dentro do grupo", results);
            s.save();
            s.data.Groups[0].Name = "Alterado";
            s.save();
            check(File.Exists(s.filePath + ".bak"), "backup antes de substituição atômica", results);
            var read = new Store(s.filePath);
            read.load("missing-seed");
            check(read.data.Groups[0].Name == "Alterado" && read.data.Groups[1].Items.Count == 2,
                  "persistência de grupos e itens", results);
            File.WriteAllText(s.filePath, "{broken");
            var recovered = new Store(s.filePath);
            recovered.load("missing");
            check(recovered.recoveryNotice != null && recovered.data.Groups[0].Name == "A",
                  "recuperar arquivo corrompido pelo backup", results);
            var restarted = new Store(s.filePath);
            restarted.load("missing");
            check(restarted.data.Groups[0].Name == "A", "persist recovery across restart", results);
            check(new Layout().StartupEnabled && !new Layout().StartupConfigured,
                  "startup enabled by default with first-run tracking", results);
            s.add(a, second);
            check(!s.move(a.Items[0].Id, b, 0), "reject duplicate target references", results);
            a.Items.Clear();
            s.data.Groups[0].Width = 9000;
            s.data.Groups[0].Height = -100;
            var bounded = Store.parse(Store.json().Serialize(s.data));
            check(bounded.Groups[0].Width == 620 && bounded.Groups[0].Height == 180,
                  "limitar tamanho dos painéis", results);
            string json = Store.json().Serialize(new Layout());
            check(Store.parse(json).Groups.Count == 0, "layout vazio válido", results);
            bool rejected = false;
            try
            {
                Store.parse("{\"Version\":2,\"Groups\":[]}");
            }
            catch
            {
                rejected = true;
            }
            check(rejected, "rejeitar versão desconhecida sem sobrescrever", results);
            s.data.Groups[0].Items.Add(new Entry { Id = id, Path = file, Name = "duplicado" });
            rejected = false;
            try
            {
                Store.parse(Store.json().Serialize(s.data));
            }
            catch
            {
                rejected = true;
            }
            check(rejected, "rejeitar identificadores duplicados", results);
            File.WriteAllText(report, String.Join(Environment.NewLine, results) + Environment.NewLine);
        }
    }
}
