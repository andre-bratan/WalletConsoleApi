using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WalletConsoleApi.Integration.Tests.TestsInfrastructure;
using WalletConsoleApi.ProcessState.Results;
using Xunit;

namespace WalletConsoleApi.Integration.Tests;

//
// WARNING: This file contains sample addresses/seed phrases/private keys. They are for testing purposes only!
// Do not use them IRL or send any assets to them - you will lose your money.
//

public class EndpointTests : IClassFixture<DevelopmentWebApplicationFactory<ProgramAssemblyMarker>>
{
    private const string TEST_COIN = "bitcoin";
    private const string TEST_MNEMONIC = "flight basket neck sword water degree trip summer dizzy other wine desk";
    private const string TEST_ADDRESS_BTC = "bc1qzx7y32wydyw77589ethf0zm7uzw6h3ea05grfz";
    private const string TEST_COIN_ERROR = "bitcoinWrong";
    private const string TEST_MNEMONIC_ERROR = "flight basket neck sword water degree trip summer dizzy other wine deskk"; // deskk
    private const string TEST_ADDRESS_BTC_ERROR = "bc1q50505050505050505050505050505050505050505050505050505050505050";
    private const string TEST_DERIVATION_PATH = "m%2F84%27%2F0%27%2F0%27%2F0%2F1"; // m/84'/0'/0'/0/1 - mind the last "1"!
    private const string TEST_PRIVATE_KEY = "c305ea940832eb344fc5bfeaa219442b64e376ac51ac057edea08b90a55ddd74";

    private readonly DevelopmentWebApplicationFactory<ProgramAssemblyMarker> factory;

    public EndpointTests(DevelopmentWebApplicationFactory<ProgramAssemblyMarker> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Test_Endpoint_Ping()
    {
        var httpClient = factory.CreateClient();

        var responseMessage = await httpClient.GetAsync("/ping");
        var response = await responseMessage.Content.ReadAsStringAsync();

        responseMessage.Should().BeSuccessful();
        response.Should().Be("Pong");
    }

    [Fact]
    public async Task Test_Endpoint_GetMnemonic()
    {
        var httpClient = factory.CreateClient();

        var responseMessage = await httpClient.GetAsync("/getMnemonic");
        var response = await responseMessage.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var mnemonicWords = response!.Result.Split(' ');

        responseMessage.Should().BeSuccessful();
        mnemonicWords.Should().HaveCount(12);
    }

    [Fact]
    public async Task Test_Endpoint_NewMnemonic()
    {
        var endpoint = "newMnemonic";

        var httpClient = factory.CreateClient();

        var responseMessageStrengthDefault = await httpClient.GetAsync($"/{endpoint}");
        var responseStrengthDefault = await responseMessageStrengthDefault.Content.ReadFromJsonAsync<StateMachineStringResult>();
        var mnemonicWordsStrengthDefault = responseStrengthDefault!.Result.Split(' ');

        var responseMessageStrength128 = await httpClient.GetAsync($"/{endpoint}?strength=128");
        var responseStrength128 = await responseMessageStrength128.Content.ReadFromJsonAsync<StateMachineStringResult>();
        var mnemonicWordsStrength128 = responseStrength128!.Result.Split(' ');

        var responseMessageStrength256 = await httpClient.GetAsync($"/{endpoint}?strength=256");
        var responseStrength256 = await responseMessageStrength256.Content.ReadFromJsonAsync<StateMachineStringResult>();
        var mnemonicWordsStrength256 = responseStrength256!.Result.Split(' ');

        var responseMessageStrengthWrong = await httpClient.GetAsync($"/{endpoint}?strength=127");
        var responseStrengthWrong = await responseMessageStrengthWrong.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageStrengthUnusual = await httpClient.GetAsync($"/{endpoint}?strength=160");
        var responseStrengthUnusual = await responseMessageStrengthUnusual.Content.ReadFromJsonAsync<StateMachineStringResult>();
        var mnemonicWordsStrengthUnusual = responseStrengthUnusual!.Result.Split(' ');

        responseMessageStrengthDefault.Should().BeSuccessful();
        mnemonicWordsStrengthDefault.Should().HaveCount(12);

        responseMessageStrength128.Should().BeSuccessful();
        mnemonicWordsStrength128.Should().HaveCount(12);

        responseMessageStrength256.Should().BeSuccessful();
        mnemonicWordsStrength256.Should().HaveCount(24);

        responseMessageStrengthWrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseStrengthWrong!.Error.Should().Be("Strength must be between 128 and 256, and multiple of 32");

        responseMessageStrengthUnusual.Should().BeSuccessful();
        mnemonicWordsStrengthUnusual.Should().HaveCount(15);
    }

    [Fact]
    public async Task Test_Endpoint_GetSeed()
    {
        var endpoint = "getSeed";

        var httpClient = factory.CreateClient();

        var responseMessageOk = await httpClient.PostAsJsonAsync($"/{endpoint}", TEST_MNEMONIC);
        var responseOk = await responseMessageOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageNoMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}", "");
        var responseNoMnemonic = await responseMessageNoMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrong = await httpClient.PostAsJsonAsync($"/{endpoint}", "wrong");
        var responseWrong = await responseMessageWrong.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}", TEST_MNEMONIC_ERROR);
        var responseInvalidMnemonic = await responseMessageInvalidMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageOk.Should().BeSuccessful();
        responseOk!.Result.Should().Be("9f2007171ba21ea19e497c7b41b23aecff348c77a16d5c3ade6fb491f4034e249e6a4aca4489c01d929e86c80b5ea10a0ed28f60226dd0d396238050deb6cce9");

        responseMessageNoMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoMnemonic!.Error.Should().Be("Mnemonic must be provided");

        responseMessageWrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrong!.Error.Should().Be("At least 12 words are needed for the mnemonic!");

        responseMessageInvalidMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidMnemonic!.Error.Should().Be("Not a valid mnemonic");
    }

    [Fact]
    public async Task Test_Endpoint_ListCoins()
    {
        var httpClient = factory.CreateClient();

        var responseMessage = await httpClient.GetAsync("/listCoins");
        var response = await responseMessage.Content.ReadFromJsonAsync<StateMachineCoinsResult>();

        responseMessage.Should().BeSuccessful();
        response!.Result.Should().HaveCountGreaterThanOrEqualTo(126);
        response!.Result.Should().Contain(x => x.Name == TEST_COIN);
    }

    [Fact]
    public async Task Test_Endpoint_CheckAddress()
    {
        var endpoint = "checkAddress";

        var httpClient = factory.CreateClient();

        var responseMessageCoin = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}/{TEST_ADDRESS_BTC}");
        var responseCoin = await responseMessageCoin.Content.ReadFromJsonAsync<StateMachineBooleanResult>();

        var responseMessageCoinWrong = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN_ERROR}/{TEST_ADDRESS_BTC}");
        var responseCoinWrong = await responseMessageCoinWrong.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageAddressWrong = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}/{TEST_ADDRESS_BTC_ERROR}");
        var responseAddressWrong = await responseMessageAddressWrong.Content.ReadFromJsonAsync<StateMachineBooleanResult>();

        responseMessageCoin.Should().BeSuccessful();
        responseCoin!.Result.Should().BeTrue();

        responseMessageCoinWrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseCoinWrong!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageAddressWrong.Should().BeSuccessful();
        responseAddressWrong!.Result.Should().BeFalse();
    }

    [Fact]
    public async Task Test_Endpoint_DerivationPath()
    {
        var httpClient = factory.CreateClient();

        var responseMessageOk = await httpClient.GetAsync($"/derivationPath/{TEST_COIN}");
        var responseOk = await responseMessageOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageWrong = await httpClient.GetAsync($"/derivationPath/{TEST_COIN_ERROR}");
        var responseWrong = await responseMessageWrong.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageOk.Should().BeSuccessful();
        responseOk!.Result.Should().Be("m/84'/0'/0'/0/0");

        responseMessageWrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrong!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");
    }

    [Fact]
    public async Task Test_Endpoint_XPub()
    {
        var endpoint = "xpub";

        var httpClient = factory.CreateClient();

        var responseMessageOk = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC);
        var responseOk = await responseMessageOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageWrongCoin = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN_ERROR}", TEST_MNEMONIC);
        var responseWrongCoin = await responseMessageWrongCoin.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageNoMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", "");
        var responseNoMnemonic = await responseMessageNoMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", "wrong");
        var responseWrongMnemonic = await responseMessageWrongMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC_ERROR);
        var responseInvalidMnemonic = await responseMessageInvalidMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageOk.Should().BeSuccessful();
        responseOk!.Result.Should().Be("zpub6rRrFHZf7WBNWFdEnpieoTCt9YFaokDmKbpeJxkPPW2eBxJuzp4yKDAnP4QRhYNBBsg3ckipRsvkBF6Y2rnm88zjVrpr7xvUhc8wVcAgJ9B");

        responseMessageWrongCoin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongCoin!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageNoMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoMnemonic!.Error.Should().Be("Mnemonic must be provided");

        responseMessageWrongMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongMnemonic!.Error.Should().Be("At least 12 words are needed for the mnemonic!");

        responseMessageInvalidMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidMnemonic!.Error.Should().Be("Not a valid mnemonic");
    }

    [Fact(Skip = "This test can take a while to complete and is for debugging purposes only")] // Read "ProcessListener -> Timer"-related code
    //[Fact]
    public async Task Test_Endpoint_XPub_UnsupportedCoin()
    {
        var endpoint = "xpub";

        var httpClient = factory.CreateClient();

        var responseMessage = await httpClient.PostAsJsonAsync($"/{endpoint}/cardano", TEST_MNEMONIC);
        var response = await responseMessage.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response!.Error.Should().Be("No data");
    }

    [Fact]
    public async Task Test_Endpoint_DefaultAddress()
    {
        var endpoint = "defaultAddress";

        var httpClient = factory.CreateClient();

        var responseMessageOk = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC);
        var responseOk = await responseMessageOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageWrongCoin = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN_ERROR}", TEST_MNEMONIC);
        var responseWrongCoin = await responseMessageWrongCoin.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageNoMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", "");
        var responseNoMnemonic = await responseMessageNoMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", "wrong");
        var responseWrongMnemonic = await responseMessageWrongMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC_ERROR);
        var responseInvalidMnemonic = await responseMessageInvalidMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageOk.Should().BeSuccessful();
        responseOk!.Result.Should().Be(TEST_ADDRESS_BTC);

        responseMessageWrongCoin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongCoin!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageNoMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoMnemonic!.Error.Should().Be("Mnemonic must be provided");

        responseMessageWrongMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongMnemonic!.Error.Should().Be("At least 12 words are needed for the mnemonic!");

        responseMessageInvalidMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidMnemonic!.Error.Should().Be("Not a valid mnemonic");
    }

    [Fact(Skip = "This test can take a while to complete and is for debugging purposes only")] // Read "ProcessListener -> Timer"-related code
    //[Fact]
    public async Task Test_Endpoint_DerivePrivateKey()
    {
        var endpoint = "derivePrivateKey";

        var httpClient = factory.CreateClient();

        var responseMessageOk = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC);
        var responseOk = await responseMessageOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageWithQueryOk = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath={TEST_DERIVATION_PATH}", TEST_MNEMONIC);
        var responseWithQueryOk = await responseMessageWithQueryOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageWrongCoin = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN_ERROR}", TEST_MNEMONIC);
        var responseWrongCoin = await responseMessageWrongCoin.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageNoMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", "");
        var responseNoMnemonic = await responseMessageNoMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", "wrong");
        var responseWrongMnemonic = await responseMessageWrongMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC_ERROR);
        var responseInvalidMnemonic = await responseMessageInvalidMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidDerivationPath = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath=wrong", TEST_MNEMONIC);
        var responseInvalidDerivationPath =
            await responseMessageInvalidDerivationPath.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageOk.Should().BeSuccessful();
        responseOk!.Result.Should().Be("634c7ec1497e7ba6016dfc8985f297ea46275dd0bab847da7590e4f79d90469e");

        responseMessageWithQueryOk.Should().BeSuccessful();
        responseWithQueryOk!.Result.Should().Be("6ac43c27276c86cd6a923adbfa09c01f3747be41f760ace525e573ce3489fee6");

        responseMessageWrongCoin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongCoin!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageNoMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoMnemonic!.Error.Should().Be("Mnemonic must be provided");

        responseMessageWrongMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongMnemonic!.Error.Should().Be("At least 12 words are needed for the mnemonic!");

        responseMessageInvalidMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidMnemonic!.Error.Should().Be("Not a valid mnemonic");

        responseMessageInvalidDerivationPath.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidDerivationPath!.Error.Should().Be("Invalid component");
    }

    [Fact(Skip = "This test can take a while to complete and is for debugging purposes only")] // Read "ProcessListener -> Timer"-related code
    //[Fact]
    public async Task Test_Endpoint_PublicKey()
    {
        var endpoint = "publicKey";

        var httpClient = factory.CreateClient();

        var responseMessageWithQueryOk = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}?privateKey={TEST_PRIVATE_KEY}");
        var responseWithQueryOk = await responseMessageWithQueryOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageNoPrivateKey = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}");
        var responseNoPrivateKey = await responseMessageNoPrivateKey.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongCoin = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN_ERROR}?privateKey={TEST_PRIVATE_KEY}");
        var responseWrongCoin = await responseMessageWrongCoin.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidPrivateKey = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}?privateKey=wrong");
        var responseInvalidPrivateKey =
            await responseMessageInvalidPrivateKey.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageWithQueryOk.Should().BeSuccessful();
        responseWithQueryOk!.Result.Should().Be("034c2635ecbc05e8b1b24a12c2f0aa6815c1ae8c946e1b9ac28db913bba0552d21");

        responseMessageNoPrivateKey.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoPrivateKey!.Error.Should().Be("Private Key must be provided");

        responseMessageWrongCoin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongCoin!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageInvalidPrivateKey.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidPrivateKey!.Error.Should().Be("Invalid private key data");
    }

    [Fact]
    public async Task Test_Endpoint_Address()
    {
        var endpoint = "address";

        var httpClient = factory.CreateClient();

        var responseMessageWithQueryOk = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}?privateKey={TEST_PRIVATE_KEY}");
        var responseWithQueryOk = await responseMessageWithQueryOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageNoPrivateKey = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}");
        var responseNoPrivateKey = await responseMessageNoPrivateKey.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongCoin = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN_ERROR}?privateKey={TEST_PRIVATE_KEY}");
        var responseWrongCoin = await responseMessageWrongCoin.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidPrivateKey = await httpClient.GetAsync($"/{endpoint}/{TEST_COIN}?privateKey=wrong");
        var responseInvalidPrivateKey =
            await responseMessageInvalidPrivateKey.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageWithQueryOk.Should().BeSuccessful();
        responseWithQueryOk!.Result.Should().Be("bc1qs8djttnstlggdxn64ezn6z0dhnkfft0rl7kqpw");

        responseMessageNoPrivateKey.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoPrivateKey!.Error.Should().Be("Private Key must be provided");

        responseMessageWrongCoin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongCoin!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageInvalidPrivateKey.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidPrivateKey!.Error.Should().Be("Invalid private key data");
    }

    [Fact]
    public async Task Test_Endpoint_DeriveAddress()
    {
        var endpoint = "deriveAddress";

        var httpClient = factory.CreateClient();

        var responseMessageWithQueryOk = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath={TEST_DERIVATION_PATH}", TEST_MNEMONIC);
        var responseWithQueryOk = await responseMessageWithQueryOk.Content.ReadFromJsonAsync<StateMachineStringResult>();

        var responseMessageNoDerivationPath = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}", TEST_MNEMONIC);
        var responseNoDerivationPath = await responseMessageNoDerivationPath.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongCoin = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN_ERROR}?derivationPath={TEST_DERIVATION_PATH}", TEST_MNEMONIC);
        var responseWrongCoin = await responseMessageWrongCoin.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageNoMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath={TEST_DERIVATION_PATH}", "");
        var responseNoMnemonic = await responseMessageNoMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageWrongMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath={TEST_DERIVATION_PATH}", "wrong");
        var responseWrongMnemonic = await responseMessageWrongMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidMnemonic = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath={TEST_DERIVATION_PATH}", TEST_MNEMONIC_ERROR);
        var responseInvalidMnemonic = await responseMessageInvalidMnemonic.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        var responseMessageInvalidDerivationPath = await httpClient.PostAsJsonAsync($"/{endpoint}/{TEST_COIN}?derivationPath=wrong", TEST_MNEMONIC);
        var responseInvalidDerivationPath =
            await responseMessageInvalidDerivationPath.Content.ReadFromJsonAsync<StateMachineErrorResult>();

        responseMessageWithQueryOk.Should().BeSuccessful();
        responseWithQueryOk!.Result.Should().Be("bc1qz8mpvpsclpcyp30lpnuh95rng98s8x73m68muy");

        responseMessageNoDerivationPath.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoDerivationPath!.Error.Should().Be("Derivation path must be provided");

        responseMessageWrongCoin.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongCoin!.Error.Should().Be($"No such coin '{TEST_COIN_ERROR}'");

        responseMessageNoMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseNoMnemonic!.Error.Should().Be("Mnemonic must be provided");

        responseMessageWrongMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseWrongMnemonic!.Error.Should().Be("At least 12 words are needed for the mnemonic!");

        responseMessageInvalidMnemonic.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidMnemonic!.Error.Should().Be("Not a valid mnemonic");

        responseMessageInvalidDerivationPath.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseInvalidDerivationPath!.Error.Should().Be("Invalid component");
    }
}
