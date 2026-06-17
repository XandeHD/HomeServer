using System.ComponentModel.DataAnnotations.Schema;

namespace HomeServer.Classes.Models
{
    [Table("group_users")]
    public class GroupUser
    {
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public string? Date { get; set; }
    }
}
