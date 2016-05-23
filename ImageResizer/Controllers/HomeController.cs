using ImageResizer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ImageResizer.Controllers
{
    public class HomeController : Controller
    {
        const int MAX_HEIGHT = 80;
        const int MAX_WIDTH = 80;

        public ActionResult Index(ImageDisplayViewModel model) {
            return View(model);
        }

        public async Task<ActionResult> SaveImage(HttpPostedFileBase originalImage) {
            var original = await SaveOriginal(originalImage.InputStream, originalImage.FileName);
            var thumb = await Resize(original, MAX_WIDTH, MAX_HEIGHT);
            return RedirectToAction("Index",
                new {
                    OriginalUrl = "/images/" + Path.GetFileName(original.FullName)
                    ,
                    ThumbUrl = "/images/thumbs/" + Path.GetFileName(thumb.FullName)
                });
        }

        private async Task<FileInfo> SaveOriginal(Stream postStream, string imageName) {
            var folder = Server.MapPath("~/images");
            var path = System.IO.Path.Combine(folder, imageName);
            using (var file = System.IO.File.OpenWrite(path)) {
                await postStream.CopyToAsync(file);
            }
            return new FileInfo(path);
        }

        private async Task<FileInfo> Resize(FileInfo original, int maxWidth, int maxHeight) {
            var folder = Server.MapPath("~/images/thumbs");
            using (var img = Image.FromFile(original.FullName)) {
                var size = await GetNewDimensions(img.Width, img.Height, maxHeight > maxWidth ? Convert.ToSingle(maxHeight) : Convert.ToSingle(maxWidth));
                var path = System.IO.Path.Combine(folder, Path.GetFileNameWithoutExtension(original.FullName) + "_" + size.Width.ToString() + "X" + size.Height.ToString() + ".jpg");
                using (var tempImage = new Bitmap(img, size.Width, size.Height)) {
                    using (var memory = new MemoryStream()) {
                        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite)) {
                            tempImage.Save(memory, ImageFormat.Jpeg);
                            byte[] bytes = memory.ToArray();
                            await fs.WriteAsync(bytes, 0, bytes.Length);
                        }
                    }
                }
                return new FileInfo(path);
            }
        }

        private async Task<Size> GetNewDimensions(int width, int height, float maxDimension) {
            if (height <= maxDimension && width <= maxDimension)
                return new Size { Height = height, Width = width };
            var flH = Convert.ToSingle(height);
            var flW = Convert.ToSingle(width);
            float factor = height > width ? flH / maxDimension : flW / maxDimension;
            var thumbHeight = Convert.ToInt32((flH / factor));
            var thumbWidth = Convert.ToInt32(flW / factor);
            return new Size { Height = thumbHeight, Width = thumbWidth };
        }
    }
}