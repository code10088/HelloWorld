public class ActivityData_Turntable : ActivityDataBase
{
    public void SetData()
    {
        //收到服务器数据

        Refresh();
    }
    public override int CheckRedPoint()
    {
        return 1;
    }
}
