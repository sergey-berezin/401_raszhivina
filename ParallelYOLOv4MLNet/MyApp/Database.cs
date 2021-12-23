using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MyApp {
    public class DetectedObject
    {
        public int DetectedObjectId { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public string Label { get; set; }
        virtual public ObjectDetails Details { get; set; }

    }
    public class ObjectDetails
    {
        public int ObjectDetailsId { get; set; }
        public byte[] Image { get; set; }
    }

    public class ImageDbContext : DbContext 
    {
        public DbSet<DetectedObject> DetectedObjects { get; set; }
        public DbSet<ObjectDetails> DetectedObjectDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o) 
            => o.UseLazyLoadingProxies().UseSqlite("Data Source=/Users/dasharazzhivina/Desktop/401_raszhivina/ParallelYOLOv4MLNet/MyApp/detectedObjects.db");

        public bool ImageInDb(DetectedObject obj)
        {
            var query = this.DetectedObjects.Where(item => item.X1 == obj.X1 && item.X2 == obj.X2 && item.Y1 == obj.Y1 && item.Y2 == obj.Y2 && item.Label == obj.Label);
            
            foreach (var item in query) {
                if (item.Details.Image.SequenceEqual(obj.Details.Image))
                {
                    return true;
                }
            }
            return false;
         }
    }
}