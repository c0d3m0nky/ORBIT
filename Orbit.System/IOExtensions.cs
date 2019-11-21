using System.IO;
using System.Threading.Tasks;

namespace Orbit {
    public static class IOExtensions
    {

        public static DirectoryInfo Subdirectory(this DirectoryInfo di, params string[] parts)
        {
            //
            // Union does not garantee preserved order
            //
            var arr = new string[parts.Length + 1];

            arr[0] = di.FullName;

            for (var i = 0; i < parts.Length; i++)
            {
                arr[i + 1] = parts[i];
            }

            return new DirectoryInfo(Path.Combine(arr));
        }

        public static FileInfo File(this DirectoryInfo di, params string[] parts)
        {
            //
            // Union does not garantee preserved order
            //
            var arr = new string[parts.Length + 1];

            arr[0] = di.FullName;

            for (var i = 0; i < parts.Length; i++)
            {
                arr[i + 1] = parts[i];
            }

            return new FileInfo(Path.Combine(arr));
        }

        public static void Rename(this DirectoryInfo di, string newName)
        {
            if (di.Parent == null)
            {
                return;
            }

            di.MoveTo(Path.Combine(di.Parent.FullName, newName));
        }

        public static void Rename(this FileInfo fi, string newName)
        {
            if (fi.Directory == null)
            {
                return;
            }

            fi.MoveTo(Path.Combine(fi.Directory.FullName, newName));
        }

        public static async Task<string> ReadAllTextAsync(this FileInfo fi)
        {
            using (var fr = fi.OpenText())
            {
                return await fr.ReadToEndAsync();
            }
        }

    }
}