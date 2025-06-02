using ParserFortTelecom.Entity;

public interface IDatabaseSaver
{
    void falseall();
    void SaveSwitches(List<SwitchData> switches);
}