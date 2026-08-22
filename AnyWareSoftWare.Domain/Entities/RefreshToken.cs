namespace AnyWareSoftWare.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
