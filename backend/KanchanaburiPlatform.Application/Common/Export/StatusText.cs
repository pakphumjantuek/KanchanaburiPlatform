namespace KanchanaburiPlatform.Application.Common.Export;

public static class StatusText
{
    public static string Order(string status)
    {
        return status switch
        {
            "Pending" => "รอยืนยันออเดอร์",
            "Confirmed" => "ยืนยันออเดอร์แล้ว",
            "Processing" => "กำลังเตรียมจัดส่ง",
            "Shipped" => "จัดส่งแล้ว",
            "Completed" => "สำเร็จสมบูรณ์",
            "Cancelled" => "ยกเลิกแล้ว",
            "Expired" => "หมดเวลาชำระเงิน",
            _ => status,
        };
    }

    public static string Payment(string status)
    {
        return status switch
        {
            "Pending" => "รอชำระเงิน",
            "PendingVerification" => "รอตรวจสอบการชำระเงิน",
            "Paid" => "ชำระเงินแล้ว",
            "Failed" => "ชำระเงินไม่สำเร็จ",
            "Rejected" => "ปฏิเสธการชำระเงิน",
            "Cancelled" => "ยกเลิกการชำระเงิน",
            "Expired" => "หมดเวลาชำระเงิน",
            _ => status,
        };
    }

    public static string Payout(string status)
    {
        return status switch
        {
            "Pending" => "รอโอนให้ร้าน",
            "Settled" => "โอนให้ร้านแล้ว",
            "Completed" => "โอนเงินสำเร็จ",
            "Failed" => "โอนเงินไม่สำเร็จ",
            "Cancelled" => "ยกเลิกการโอนเงิน",
            _ => status,
        };
    }
}
