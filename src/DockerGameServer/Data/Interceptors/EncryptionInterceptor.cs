using DockerGameServer.Models.Database;
using DockerGameServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DockerGameServer.Data.Interceptors
{
    public sealed class EncryptionInterceptor(EncryptionService encryptionService) : SaveChangesInterceptor, IMaterializationInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
        {
            var context = eventData.Context;
            if (context == null)
                return result;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                    EncryptIfEncryptedEntity(entry.Entity);
            }

            return result;
        }

        public object InitializedInstance(
        MaterializationInterceptionData data,
        object entity)
        {
            DecryptIfEncryptedEntity(entity);
            return entity;
        }

        private void EncryptIfEncryptedEntity(object entity)
        {
            var type = entity.GetType();
            var baseType = type.BaseType;

            if (baseType == null || !baseType.IsGenericType)
                return;

            if (baseType.GetGenericTypeDefinition() != typeof(EncryptedDataEntity<>))
                return;

            var dataProp = baseType.GetProperty("Data");
            var data = dataProp?.GetValue(entity);
            if (data == null)
                return;

            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);

            var result = encryptionService.Encrypt(bytes);

            type.GetProperty("EncryptedDataValue")?.SetValue(entity, result.EncryptedValue);
            type.GetProperty("DataValueNonce")?.SetValue(entity, result.ValueNonce);
            type.GetProperty("EncryptedDataKey")?.SetValue(entity, result.EncryptedDataKey);
            type.GetProperty("DataKeyNonce")?.SetValue(entity, result.DataKeyNonce);
        }

        private void DecryptIfEncryptedEntity(object entity)
        {
            var type = entity.GetType();
            var baseType = type.BaseType;

            if (baseType == null || !baseType.IsGenericType)
                return;

            if (baseType.GetGenericTypeDefinition() != typeof(EncryptedDataEntity<>))
                return;

            var encryptedValue = (byte[])type.GetProperty("EncryptedDataValue")?.GetValue(entity)!;
            var valueNonce = (byte[])type.GetProperty("DataValueNonce")?.GetValue(entity)!;
            var encryptedKey = (byte[])type.GetProperty("EncryptedDataKey")?.GetValue(entity)!;
            var keyNonce = (byte[])type.GetProperty("DataKeyNonce")?.GetValue(entity)!;

            if (encryptedValue == null || encryptedValue.Length == 0)
                return;

            var json = encryptionService.Decrypt(encryptedValue, encryptedKey, valueNonce, keyNonce);

            var payloadType = baseType.GetGenericArguments()[0];

            var payload = JsonSerializer.Deserialize(json.Value, payloadType);

            baseType.GetProperty("Data")?.SetValue(entity, payload);
        }
    }
}
