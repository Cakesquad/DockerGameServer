using System.ComponentModel.DataAnnotations.Schema;

namespace DockerGameServer.Models.Database
{
    public abstract class EncryptedDataEntity<T>: BaseEntity where T : new()
    {
        [NotMapped]
        public T Data { get; set; } = new T();

        public byte[] EncryptedDataValue { get; set; } = default!;
        public byte[] DataValueNonce { get; set; } = default!;
        public byte[] EncryptedDataKey { get; set; } = default!;
        public byte[] DataKeyNonce { get; set; } = default!;
    }
}
