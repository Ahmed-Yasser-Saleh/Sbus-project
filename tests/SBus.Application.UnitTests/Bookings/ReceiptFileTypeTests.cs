using System.Text;

using SBus.Application.Features.Bookings.Common;

using Xunit;

namespace SBus.Application.UnitTests.Bookings;

public class ReceiptFileTypeTests
{
    [Fact]
    public void DetectExtension_Jpeg()
    {
        Assert.Equal(".jpg", ReceiptFileType.DetectExtension([0xFF, 0xD8, 0xFF, 0xE0, 0, 0]));
    }

    [Fact]
    public void DetectExtension_Png()
    {
        Assert.Equal(".png", ReceiptFileType.DetectExtension([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0]));
    }

    [Fact]
    public void DetectExtension_Webp()
    {
        Assert.Equal(".webp", ReceiptFileType.DetectExtension(Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 ")));
    }

    [Fact]
    public void DetectExtension_Pdf()
    {
        Assert.Equal(".pdf", ReceiptFileType.DetectExtension(Encoding.ASCII.GetBytes("%PDF-1.7\n")));
    }

    [Fact]
    public void DetectExtension_HtmlRenamedToPng_IsRejected()
    {
        Assert.Null(ReceiptFileType.DetectExtension(Encoding.ASCII.GetBytes("<html><script>")));
    }
}
