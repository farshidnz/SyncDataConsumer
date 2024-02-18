using NUnit.Framework;
using Moq;
using AccountSyncData.Consumer.Encryption;

namespace AccountSyncData.Unit.Tests;

internal class SHACryptorTest
{
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(15)]
    public void GenerateSaltKey_SizeIs10_ShouldReturn10CharacterString(int size)
    {
        var result = SHACryptor.GenerateSaltKey(size);
        Assert.AreEqual(size, result.Length);
    }
}
