# Release signing

Debug builds (`dotnet build -t:Run`) are unaffected by any of this — they always use the
auto-generated debug keystore, same as before.

To produce a Release APK you can actually sideload (or eventually publish), you need your own
release keystore. Generate it once, keep it forever — losing it means any future update can never
be installed over an existing install signed with it, and if this is ever published to Play Store,
losing it means losing the ability to update the app at all.

## One-time setup

```sh
cd WeighIn/signing
keytool -genkeypair -v \
  -keystore weighin-release.keystore \
  -alias weighin \
  -keyalg RSA -keysize 2048 -validity 10950 \
  -dname "CN=Your Name, OU=WeighIn, O=WeighIn, L=City, ST=State, C=US"
```

`keytool` will prompt you to set a keystore password and a key password — pick strong, unique
values and store them in a password manager. `-validity 10950` is 30 years.

Then:

```sh
cp signing.local.props.template signing.local.props
```

Edit `signing.local.props` and fill in the same two passwords you just chose, plus confirm the
alias matches (`weighin`, if you used the command above unchanged).

## Building a signed Release APK

```sh
dotnet build WeighIn.csproj -f net10.0-android -c Release
```

The signed APK lands under `bin/Release/net10.0-android/`.

## What's committed vs. not

- `signing.local.props.template` and this README are committed — they're just documentation.
- `weighin-release.keystore` and `signing.local.props` (the real one, with real passwords) are
  gitignored and must never be committed. Back them up somewhere safe outside git — a password
  manager or encrypted storage, not a repo.
