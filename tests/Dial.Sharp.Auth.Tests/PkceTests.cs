using System.Security.Cryptography;
using System.Text;
using Dial.Sharp.Auth.Internal;

namespace Dial.Sharp.Auth;

public class PkceTests
{
    [Fact]
    public void Create_ProducesUrlSafeVerifierAndS256Challenge()
    {
        var (verifier, challenge) = Pkce.Create();

        Assert.False(string.IsNullOrEmpty(verifier));
        Assert.DoesNotContain('+', verifier);
        Assert.DoesNotContain('/', verifier);
        Assert.DoesNotContain('=', verifier);

        var expected = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(expected, challenge);
    }

    [Fact]
    public void Create_ProducesDistinctVerifiers()
    {
        var (first, _) = Pkce.Create();
        var (second, _) = Pkce.Create();
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CreateState_IsUrlSafeAndNonEmpty()
    {
        var state = Pkce.CreateState();
        Assert.False(string.IsNullOrEmpty(state));
        Assert.DoesNotContain('=', state);
    }
}
