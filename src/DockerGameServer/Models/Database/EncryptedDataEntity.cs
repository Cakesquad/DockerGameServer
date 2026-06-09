using System.ComponentModel.DataAnnotations.Schema;

namespace DockerGameServer.Models.Database
{
    public abstract class EncryptedDataEntity<T>: BaseEntity where T : new()
    {
        [NotMapped]
        public T Data { get; set; } = new T();

        public byte[] EncryptedDataValue { get; set; } = [];
        public byte[] DataValueNonce { get; set; } = [];
        public byte[] EncryptedDataKey { get; set; } = [];
        public byte[] DataKeyNonce { get; set; } = [];
    }
}
