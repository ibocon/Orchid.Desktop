// keep: CultureInfo for CA1305 invariant SVG coordinate formatting
using System.Globalization;
// keep: StringBuilder and Encoding for deterministic QR SVG generation
using System.Text;

namespace Orchid.Desktop.Service;

/// <summary>
/// 페어링 키 페이로드 문자열을 스캔 가능한 표준 QR 코드 SVG 마크업으로 변환합니다.
/// </summary>
public static class PairingQrCodeSvgGenerator
{
    private const int Version4Size = 33;
    private const int QuietZoneModules = 4;
    private const int DataCodewordCount = 80;
    private const int EccCodewordCount = 20;
    private const int MaxBytePayloadLength = 78;

    // ECC Level L (01) 및 Mask 0 (000)의 BCH(15,5)와 0x5412 마스킹이 적용된 15비트 포맷 정보 상수
    private const int FormatBitsLevelLMask0 = 0b111011111000100;

    /// <summary>
    /// 지정한 페어링 페이로드 문자열을 QR 코드 SVG 문자열로 변환합니다.
    /// </summary>
    /// <param name="payload">직렬화된 페어링 키 페이로드</param>
    /// <returns>렌더링 가능한 SVG 마크업 문자열</returns>
    public static string GenerateSvg(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        if (payloadBytes.Length > MaxBytePayloadLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payload),
                $"QR Version 4-L Byte mode supports up to {MaxBytePayloadLength} bytes.");
        }

        bool[,] matrix = BuildQrMatrix(payloadBytes);
        int size = matrix.GetLength(0);
        int totalModules = size + (QuietZoneModules * 2);

        StringBuilder pathBuilder = new();
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                if (matrix[row, col])
                {
                    int x = col + QuietZoneModules;
                    int y = row + QuietZoneModules;
                    pathBuilder.Append(CultureInfo.InvariantCulture, $"M{x},{y}h1v1h-1z");
                }
            }
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalModules} {totalModules}\" width=\"168\" height=\"168\" shape-rendering=\"crispEdges\" role=\"img\" aria-label=\"Pairing QR Code\"><rect width=\"{totalModules}\" height=\"{totalModules}\" rx=\"2\" fill=\"#FFFFFF\" /><path d=\"{pathBuilder}\" fill=\"#111827\" /></svg>");
    }

    private static bool[,] BuildQrMatrix(ReadOnlySpan<byte> payloadBytes)
    {
        bool[,] modules = new bool[Version4Size, Version4Size];
        bool[,] isFunction = new bool[Version4Size, Version4Size];

        DrawFunctionPatterns(modules, isFunction);

        byte[] dataCodewords = EncodeDataCodewords(payloadBytes);
        byte[] eccCodewords = ComputeReedSolomonEcc(dataCodewords);

        byte[] allCodewords = new byte[DataCodewordCount + EccCodewordCount];
        Buffer.BlockCopy(dataCodewords, 0, allCodewords, 0, DataCodewordCount);
        Buffer.BlockCopy(eccCodewords, 0, allCodewords, DataCodewordCount, EccCodewordCount);

        PlaceCodewordsAndApplyMask0(modules, isFunction, allCodewords);
        return modules;
    }

    private static byte[] EncodeDataCodewords(ReadOnlySpan<byte> payloadBytes)
    {
        byte[] codewords = new byte[DataCodewordCount];
        int bitIndex = 0;

        AppendBits(codewords, ref bitIndex, 0b0100, 4);
        AppendBits(codewords, ref bitIndex, payloadBytes.Length, 8);

        for (int i = 0; i < payloadBytes.Length; i++)
        {
            AppendBits(codewords, ref bitIndex, payloadBytes[i], 8);
        }

        int capacityBits = DataCodewordCount * 8;
        int terminatorLength = Math.Min(4, capacityBits - bitIndex);
        AppendBits(codewords, ref bitIndex, 0, terminatorLength);

        int remainderBits = (8 - (bitIndex % 8)) % 8;
        AppendBits(codewords, ref bitIndex, 0, remainderBits);

        byte[] padBytes = [0xEC, 0x11];
        int padToggle = 0;
        while (bitIndex < capacityBits)
        {
            AppendBits(codewords, ref bitIndex, padBytes[padToggle], 8);
            padToggle ^= 1;
        }

        return codewords;
    }

    private static void AppendBits(byte[] buffer, ref int bitIndex, int value, int bitCount)
    {
        for (int i = bitCount - 1; i >= 0; i--)
        {
            int bit = (value >> i) & 1;
            if (bit != 0)
            {
                buffer[bitIndex >> 3] |= (byte)(1 << (7 - (bitIndex & 7)));
            }

            bitIndex++;
        }
    }

    private static byte[] ComputeReedSolomonEcc(ReadOnlySpan<byte> data)
    {
        // GF(256) 원시다항식 0x11D 위에서 근 alpha^0 .. alpha^(EccCodewordCount - 1)을 갖는 생성다항식으로 Reed-Solomon 오류정정 코드워드를 계산한다
        byte[] generator = new byte[EccCodewordCount];
        generator[EccCodewordCount - 1] = 1;

        byte root = 1;
        for (int i = 0; i < EccCodewordCount; i++)
        {
            for (int j = 0; j < EccCodewordCount; j++)
            {
                generator[j] = GfMultiply(generator[j], root);
                if (j + 1 < EccCodewordCount)
                {
                    generator[j] ^= generator[j + 1];
                }
            }

            root = GfMultiply(root, 0x02);
        }

        byte[] ecc = new byte[EccCodewordCount];
        for (int i = 0; i < data.Length; i++)
        {
            byte factor = (byte)(data[i] ^ ecc[0]);
            Array.Copy(ecc, 1, ecc, 0, EccCodewordCount - 1);
            ecc[EccCodewordCount - 1] = 0;

            for (int j = 0; j < EccCodewordCount; j++)
            {
                ecc[j] ^= GfMultiply(generator[j], factor);
            }
        }

        return ecc;
    }

    private static byte GfMultiply(byte x, byte y)
    {
        int result = 0;
        for (int i = 7; i >= 0; i--)
        {
            result = (result << 1) ^ ((result >> 7) * 0x11D);
            result ^= ((y >> i) & 1) * x;
        }

        return (byte)result;
    }

    private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction)
    {
        for (int i = 0; i < Version4Size; i++)
        {
            SetFunctionModule(modules, isFunction, 6, i, (i & 1) == 0);
            SetFunctionModule(modules, isFunction, i, 6, (i & 1) == 0);
        }

        DrawFinderPattern(modules, isFunction, 3, 3);
        DrawFinderPattern(modules, isFunction, 3, Version4Size - 4);
        DrawFinderPattern(modules, isFunction, Version4Size - 4, 3);
        DrawAlignmentPattern(modules, isFunction, 26, 26);
        DrawFormatBits(modules, isFunction);
    }

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int centerRow, int centerCol)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int row = centerRow + dy;
                int col = centerCol + dx;
                if (row is >= 0 and < Version4Size && col is >= 0 and < Version4Size)
                {
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    bool isDark = dist is not 2 and not 4;
                    SetFunctionModule(modules, isFunction, row, col, isDark);
                }
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int centerRow, int centerCol)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                SetFunctionModule(modules, isFunction, centerRow + dy, centerCol + dx, dist != 1);
            }
        }
    }

    private static void DrawFormatBits(bool[,] modules, bool[,] isFunction)
    {
        for (int i = 0; i <= 5; i++)
        {
            SetFunctionModule(modules, isFunction, i, 8, GetBit(FormatBitsLevelLMask0, i));
        }

        SetFunctionModule(modules, isFunction, 7, 8, GetBit(FormatBitsLevelLMask0, 6));
        SetFunctionModule(modules, isFunction, 8, 8, GetBit(FormatBitsLevelLMask0, 7));
        SetFunctionModule(modules, isFunction, 8, 7, GetBit(FormatBitsLevelLMask0, 8));

        for (int i = 9; i < 15; i++)
        {
            SetFunctionModule(modules, isFunction, 8, 14 - i, GetBit(FormatBitsLevelLMask0, i));
        }

        for (int i = 0; i < 8; i++)
        {
            SetFunctionModule(modules, isFunction, 8, Version4Size - 1 - i, GetBit(FormatBitsLevelLMask0, i));
        }

        for (int i = 8; i < 15; i++)
        {
            SetFunctionModule(modules, isFunction, Version4Size - 15 + i, 8, GetBit(FormatBitsLevelLMask0, i));
        }

        SetFunctionModule(modules, isFunction, Version4Size - 8, 8, true);
    }

    private static void PlaceCodewordsAndApplyMask0(bool[,] modules, bool[,] isFunction, ReadOnlySpan<byte> allCodewords)
    {
        int bitIndex = 0;
        int totalBits = allCodewords.Length * 8;

        for (int right = Version4Size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }

            bool upward = ((right + 1) & 2) == 0;
            for (int vert = 0; vert < Version4Size; vert++)
            {
                int row = upward ? Version4Size - 1 - vert : vert;
                for (int j = 0; j < 2; j++)
                {
                    int col = right - j;
                    if (isFunction[row, col])
                    {
                        continue;
                    }

                    bool dataBit = false;
                    if (bitIndex < totalBits)
                    {
                        dataBit = ((allCodewords[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                        bitIndex++;
                    }

                    bool maskInvert = ((row + col) & 1) == 0;
                    modules[row, col] = dataBit ^ maskInvert;
                }
            }
        }
    }

    private static void SetFunctionModule(bool[,] modules, bool[,] isFunction, int row, int col, bool isDark)
    {
        modules[row, col] = isDark;
        isFunction[row, col] = true;
    }

    private static bool GetBit(int value, int bitIndex)
    {
        return ((value >> bitIndex) & 1) != 0;
    }
}
