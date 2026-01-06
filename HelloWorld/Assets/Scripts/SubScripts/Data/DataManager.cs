public interface DataBase
{
    void Init();
    void Clear();
}
public class DataManager : Singletion<DataManager>
{
    private ActivityData activityData;
    private GuideData guideData;
    private HotUpdateResData hotUpdateResData;
    private MailData mailData;
    private PlayerData playerData;
    private TestData testData;

    public ActivityData ActivityData
    {
        get
        {
            if (activityData == null)
            {
                activityData = new ActivityData();
                activityData.Init();
            }
            return activityData;
        }
    }
    public GuideData GuideData
    {
        get
        {
            if (guideData == null)
            {
                guideData = new GuideData();
                guideData.Init();
            }
            return guideData;
        }
    }
    public HotUpdateResData HotUpdateResData
    {
        get
        {
            if (hotUpdateResData == null)
            {
                hotUpdateResData = new HotUpdateResData();
                hotUpdateResData.Init();
            }
            return hotUpdateResData;
        }
    }
    public MailData MailData
    {
        get
        {
            if (mailData == null)
            {
                mailData = new MailData();
                mailData.Init();
            }
            return mailData;
        }
    }
    public PlayerData PlayerData
    {
        get
        {
            if (playerData == null)
            {
                playerData = new PlayerData();
                playerData.Init();
            }
            return playerData;
        }
    }
    public TestData TestData
    {
        get
        {
            if (testData == null)
            {
                testData = new TestData();
                testData.Init();
            }
            return testData;
        }
    }

    public void Clear()
    {
        activityData?.Clear();
        guideData?.Clear();
        hotUpdateResData?.Clear();
        mailData?.Clear();
        playerData?.Clear();
        testData?.Clear();
    }
}