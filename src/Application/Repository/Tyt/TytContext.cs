namespace SurveillanceCameras.Application.Repository.Tyt;

/// <param name="AccountId">Tài khoản TYT dùng để đăng truyện (phiên Chrome của tài khoản này gửi request).</param>
/// <param name="BaseUrl">
/// Địa chỉ TYT lấy từ <c>AccountType.BaseUrl</c> của tài khoản (vd. https://tytnovel.info/) — domain đổi thì chỉ cần
/// sửa loại tài khoản, không sửa code.
/// </param>
/// <param name="LoginCookieName">Cookie nhận biết đã đăng nhập (<c>AccountType.LoginCookieName</c>).</param>
public record TytContext(int AccountId, string BaseUrl, string? LoginCookieName);
