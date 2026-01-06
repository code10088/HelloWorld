public interface DataBase
{
    void Init();
    void Clear();
}
public partial class DataManager : Singletion<DataManager>
{

}