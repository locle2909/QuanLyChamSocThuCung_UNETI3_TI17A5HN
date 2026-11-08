// Họ và tên: Lê Hữu Lộc
// Mã sinh viên: 23103100283
// Nội dung thực hiện: Dịch vụ gửi email (MailKit). Chưa cấu hình thì ghi liên kết ra log để vẫn thử được.
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Helpers;

public class EmailSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
}

public class EmailService
{
    private readonly EmailSettings _cfg;
    private readonly ILogger<EmailService> _log;

    public EmailService(IOptions<EmailSettings> cfg, ILogger<EmailService> log)
    {
        _cfg = cfg.Value;
        _log = log;
    }

    public bool DaCauHinh =>
        !string.IsNullOrWhiteSpace(_cfg.Host) &&
        !string.IsNullOrWhiteSpace(_cfg.User) &&
        !string.IsNullOrWhiteSpace(_cfg.Password);

    // Trả về true nếu đã gửi email thật. lienKetDuPhong chỉ được ghi ra log khi chưa cấu hình.
    public async Task<bool> GuiAsync(string den, string tieuDe, string noiDungHtml, string lienKetDuPhong)
    {
        if (!DaCauHinh)
        {
            _log.LogWarning("Chua cau hinh email. Lien ket gui toi {Den}: {LienKet}", den, lienKetDuPhong);
            return false;
        }

        try
        {
            var thu = new MimeMessage();
            thu.From.Add(new MailboxAddress("PetCare", string.IsNullOrWhiteSpace(_cfg.From) ? _cfg.User : _cfg.From));
            thu.To.Add(MailboxAddress.Parse(den));
            thu.Subject = tieuDe;
            thu.Body = new BodyBuilder { HtmlBody = noiDungHtml }.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_cfg.Host, _cfg.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_cfg.User, _cfg.Password);
            await smtp.SendAsync(thu);
            await smtp.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gui email that bai toi {Den}", den);
            _log.LogWarning("Lien ket du phong: {LienKet}", lienKetDuPhong);
            return false;
        }
    }
}