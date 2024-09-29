namespace UnfoldedCircle.Models.Sync;

/// <summary>
/// <para>Authentication response.</para>
/// <para>
/// The authentication result is provided in the `code` attribute: - `200`: success, API can be used and message requests are accepted. - `401`: authentication failed, the provided token is not valid. The UC Remote will close the connection. The driver should also close the connection after sending this response. If the driver doesn't support or require authentication, it still needs to send the `authentication` message with `code: 200` and `req_id: 0` after the WebSocket connection has been established by the UC Remote. It's recommended to send the optional driver version object in the the msg_data payload to avoid eventual additional message exchanges.
/// </para>
/// </summary>
public record AuthMsg : CommonRespOptional<DriverVersion>;