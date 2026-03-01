using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace photo_cleaner_helper.Models
{
    public class PhotoItem
    {
        public string FilePath { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
