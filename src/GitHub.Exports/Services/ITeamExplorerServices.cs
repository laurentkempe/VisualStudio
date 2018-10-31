using System.Windows.Input;

namespace GitHub.Services
{
    public interface ITeamExplorerServices : INotificationService
    {
        void ShowConnectPage();
        void ShowCommitDetails(string oid);
        void ShowHomePage();
        void ShowPublishSection();
        void ClearNotifications();
        void OpenRepository(string repositoryPath);
    }
}