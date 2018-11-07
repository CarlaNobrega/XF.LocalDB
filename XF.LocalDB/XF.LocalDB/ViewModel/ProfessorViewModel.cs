using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using XF.LocalDB.Model;

namespace XF.LocalDB.ViewModel
{
    public class ProfessorViewModel
    {      
        public List<Professor> CopiaListaProfessores { get; private set; }

         #region Propriedades
        public Professor ProfessoroModel { get; set; }
        public string Nome { get { return App.UsuarioVM.Nome; } }

        private Professor selecionado;
        public Professor Selecionado
        {
            get { return selecionado; }
            set
            {
                selecionado = value as Professor;
                EventPropertyChanged();
            }
        }

        private string pesquisaPorNome;
        public string PesquisaPorNome
        {
            get { return pesquisaPorNome; }
            set
            {
                if (value == pesquisaPorNome) return;

                pesquisaPorNome = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PesquisaPorNome)));
                AplicarFiltro();
            }
        }
               
        public ObservableCollection<Professor> Professores { get; set; } = new ObservableCollection<Professor>();

        // UI Events
        public OnAdicionarProfessorCMD OnAdicionarProfessorCMD { get; }
        public OnEditarProfessorCMD OnEditarProfessorCMD { get; }
        public OnDeleteProfessorCMD OnDeleteProfessorCMD { get; }
        public ICommand OnSairCMD { get; private set; }
        public ICommand OnNovoCMD { get; private set; }

        #endregion

        public ProfessorViewModel()
        {
            var teste = ProfessorRepository.GetProfessoresSqlAzureAsync();
            OnAdicionarProfessorCMD = new OnAdicionarProfessorCMD(this);
            OnEditarProfessorCMD = new OnEditarProfessorCMD(this);
            OnDeleteProfessorCMD = new OnDeleteProfessorCMD(this);
            OnSairCMD = new Xamarin.Forms.Command(OnSair);
            OnNovoCMD = new Xamarin.Forms.Command(OnNovo);

            CopiaListaProfessores = new List<Professor>();
            Carregar();
        }

        public void Carregar()
        {
            CopiaListaProfessores = await ProfessorRepository.GetProfessoresSqlAzureAsync();
            AplicarFiltro();
        }

        private void AplicarFiltro()
        {
            if (pesquisaPorNome == null)
                pesquisaPorNome = "";

            var resultado = CopiaListaProfessores.Where(n => n.Nome.ToLowerInvariant().Contains(PesquisaPorNome.ToLowerInvariant().Trim())).ToList();

            var removerDaLista = Professores.Except(resultado).ToList();
            foreach (var item in removerDaLista)
            {
                Professores.Remove(item);
            }

            for (int index = 0; index < resultado.Count; index++)
            {
                var item = resultado[index];
                if (index + 1 > Professores.Count || !Professores[index].Equals(item))
                    Professores.Insert(index, item);
            }
        }

        public void Adicionar(Professor paramProfessor)
        {
            if ((paramProfessor == null) || (string.IsNullOrWhiteSpace(paramProfessor.Nome)))
                App.Current.MainPage.DisplayAlert("Atenção", "O campo nome é obrigatório", "OK");
            else if (ProfessorRepository.PostProfessorSqlAzureAsync(paramProfessor))
                App.Current.MainPage.Navigation.PopAsync();
            else
                App.Current.MainPage.DisplayAlert("Falhou", "Desculpe, ocorreu um erro inesperado =(", "OK");
        }

        public async void Editar()
        {
            await App.Current.MainPage.Navigation.PushAsync(
                new View.Aluno.NovoAlunoView() { BindingContext = App.AlunoVM });
        }

        public async void Remover()
        {
            if (await App.Current.MainPage.DisplayAlert("Atenção?",
                string.Format("Tem certeza que deseja remover o {0}?", Selecionado.Nome), "Sim", "Não"))
            {
                if (ProfessorRepository.DeleteProfessorSqlAzureAsync(Selecionado.Id))
                {
                    CopiaListaProfessores.Remove(Selecionado);
                    Carregar();
                }
                else
                    await App.Current.MainPage.DisplayAlert(
                            "Falhou", "Desculpe, ocorreu um erro inesperado =(", "OK");
            }
        }

        private async void OnSair()
        {
            await App.Current.MainPage.Navigation.PopAsync();
        }

        private void OnNovo()
        {
            App.AlunoVM.Selecionado = new Model.Aluno();
            App.Current.MainPage.Navigation.PushAsync(
                new View.Aluno.NovoAlunoView() { BindingContext = App.AlunoVM });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void EventPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class OnAdicionarProfessorCMD : ICommand
    {
        private ProfessorViewModel alunoVM;
        public OnAdicionarProfessorCMD(ProfessorViewModel paramVM)
        {
            alunoVM = paramVM;
        }
        public event EventHandler CanExecuteChanged;
        public void AdicionarCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter)
        {
            alunoVM.Adicionar(parameter as Professor);
        }
    }

    public class OnEditarProfessorCMD : ICommand
    {
        private ProfessorViewModel professoroVM;
        public OnEditarProfessorCMD(ProfessorViewModel paramVM)
        {
            professoroVM = paramVM;
        }
        public event EventHandler CanExecuteChanged;
        public void EditarCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(object parameter) => (parameter != null);
        public void Execute(object parameter)
        {
            App.ProfessorVM.Selecionado = parameter as Professor;
            professoroVM.Editar();
        }
    }

    public class OnDeleteProfessorCMD : ICommand
    {
        private ProfessorViewModel professorVM;
        public OnDeleteProfessorCMD(ProfessorViewModel paramVM)
        {
            professorVM = paramVM;
        }
        public event EventHandler CanExecuteChanged;
        public void DeleteCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(object parameter) => (parameter != null);
        public void Execute(object parameter)
        {
            App.ProfessorVM.Selecionado = parameter as Professor;
            professorVM.Remover();
        }
    }

}

