using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace CompressImagesFunction
{
    public class CommitSignature
    {
        public static string Sign(string commitMessage, string privateKey, string password)
        {
            using (var privateKeyStream = new MemoryStream(Encoding.ASCII.GetBytes(privateKey)))
            using (var outputStream = new MemoryStream())
            {
                var signedMessage = DoSigning(commitMessage, privateKeyStream, outputStream, password.ToCharArray());

                // cutoff the actual message, we just want the signature
                return signedMessage.Substring(signedMessage.IndexOf("-----BEGIN PGP SIGNATURE", StringComparison.Ordinal));
            }
        }

        private static string DoSigning(string input, Stream keyIn, Stream outputStream, char[] pass)
        {
            var digest = HashAlgorithmTag.Sha256;
            var pgpSecretKey = ReadSigningSecretKey(keyIn);
            var pgpPrivateKey = pgpSecretKey.ExtractPrivateKey(pass);
            var signatureGenerator = new PgpSignatureGenerator(pgpSecretKey.PublicKey.Algorithm, digest);
            var subpacketGenerator = new PgpSignatureSubpacketGenerator();

            signatureGenerator.InitSign(PgpSignature.StandAlone, pgpPrivateKey);

            foreach (var userId in pgpSecretKey.PublicKey.GetUserIds())
            {
                subpacketGenerator.SetSignerUserId(false, userId.ToString());
                signatureGenerator.SetHashedSubpackets(subpacketGenerator.Generate());
            }

            using (var inputStream = new MemoryStream(Encoding.ASCII.GetBytes(input)))
            using (var armoredOut = new ArmoredOutputStream(outputStream))
            {
                armoredOut.BeginClearText(digest);

                // note the last \n/\r/\r\n in the file is ignored
                using (var lineOut = new MemoryStream())
                {
                    int lookAhead = ReadInputLine(lineOut, inputStream);
                    ProcessLine(armoredOut, signatureGenerator, lineOut.ToArray());

                    if (lookAhead != -1)
                    {
                        do
                        {
                            lookAhead = ReadInputLine(lineOut, lookAhead, inputStream);
                            signatureGenerator.Update((byte)'\n');
                            ProcessLine(armoredOut, signatureGenerator, lineOut.ToArray());
                        }
                        while (lookAhead != -1);
                    }

                    inputStream.Close();
                    armoredOut.EndClearText();

                    using (var bcpgOutput = new BcpgOutputStream(armoredOut))
                    {
                        signatureGenerator.Generate().Encode(bcpgOutput);
                        armoredOut.Close();
                        outputStream.Seek(0, 0);
                        return new StreamReader(outputStream).ReadToEnd();
                    }
                }
            }
        }

        private static PgpSecretKeyRingBundle CreatePgpSecretKeyRingBundle(Stream keyInStream)
        {
            return new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(keyInStream));
        }

        private static PgpSecretKey ReadSigningSecretKey(System.IO.Stream keyInStream)
        {
            PgpSecretKeyRingBundle pgpSec = CreatePgpSecretKeyRingBundle(keyInStream);
            PgpSecretKey key = null;
            System.Collections.IEnumerator rIt = pgpSec.GetKeyRings().GetEnumerator();
            while (key == null && rIt.MoveNext())
            {
                PgpSecretKeyRing kRing = (PgpSecretKeyRing)rIt.Current;
                System.Collections.IEnumerator kIt = kRing.GetSecretKeys().GetEnumerator();
                while (key == null && kIt.MoveNext())
                {
                    PgpSecretKey k = (PgpSecretKey)kIt.Current;
                    if (k.IsSigningKey)
                        key = k;
                }
            }

            if (key == null)
                throw new System.Exception("Wrong private key - Can't find signing key in key ring.");
            else
                return key;
        }

        private static int ReadInputLine(MemoryStream bOut, Stream fIn)
        {
            bOut.SetLength(0);

            int lookAhead = -1;
            int ch;

            while ((ch = fIn.ReadByte()) >= 0)
            {
                bOut.WriteByte((byte)ch);
                if (ch == '\r' || ch == '\n')
                {
                    lookAhead = ReadPassedEol(bOut, ch, fIn);
                    break;
                }
            }

            return lookAhead;
        }

        private static int ReadInputLine(
           MemoryStream bOut,
           int lookAhead,
           Stream fIn)
        {
            bOut.SetLength(0);

            int ch = lookAhead;

            do
            {
                bOut.WriteByte((byte)ch);
                if (ch == '\r' || ch == '\n')
                {
                    lookAhead = ReadPassedEol(bOut, ch, fIn);
                    break;
                }
            }
            while ((ch = fIn.ReadByte()) >= 0);

            if (ch < 0)
            {
                lookAhead = -1;
            }

            return lookAhead;
        }

        private static int ReadPassedEol(MemoryStream bOut, int lastCh, Stream fIn)
        {
            int lookAhead = fIn.ReadByte();

            if (lastCh == '\r' && lookAhead == '\n')
            {
                bOut.WriteByte((byte)lookAhead);
                lookAhead = fIn.ReadByte();
            }

            return lookAhead;
        }

        private static void ProcessLine(Stream aOut, PgpSignatureGenerator sGen, byte[] line)
        {
            int length = GetLengthWithoutWhiteSpace(line);
            if (length > 0)
            {
                sGen.Update(line, 0, length);
            }

            aOut.Write(line, 0, line.Length);
        }

        private static void ProcessLine(PgpSignature sig, byte[] line)
        {
            // note: trailing white space needs to be removed from the end of
            // each line for signature calculation RFC 4880 Section 7.1
            int length = GetLengthWithoutWhiteSpace(line);
            if (length > 0)
            {
                sig.Update(line, 0, length);
            }
        }

        private static int GetLengthWithoutWhiteSpace(byte[] line)
        {
            int end = line.Length - 1;

            while (end >= 0 && IsWhiteSpace(line[end]))
            {
                end--;
            }

            return end + 1;
        }

        private static bool IsWhiteSpace(byte b)
        {
            return IsLineEnding(b) || b == '\t' || b == ' ';
        }

        private static bool IsLineEnding(byte b)
        {
            return b == '\r' || b == '\n';
        }
    }
}
