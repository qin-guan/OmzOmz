using Stateless;
using Stateless.Graph;

namespace OmzOmz.WebApi.StateMachines;

public class Chat
{
    public enum State
    {
        Start,
        EditingProfileName,
        EditingProfileAge,
        EditingProfileDescription,
        EditingProfilePicture,
        ViewingProfileSummary,
        ViewingProfiles
    }

    private enum Trigger
    {
        EditProfileName,
        EditProfileAge,
        EditProfileDescription,
        EditProfilePicture,
        ViewProfileSummary,
        ViewProfiles
    }

    private readonly StateMachine<State, Trigger> _machine;

    public State CurrentState { get; set; } = State.Start;

    public Chat()
    {
        _machine = new StateMachine<State, Trigger>(() => CurrentState, state => CurrentState = state);

        _machine.Configure(State.Start)
            .Permit(Trigger.EditProfileName, State.EditingProfileName);

        _machine.Configure(State.EditingProfileName)
            .Permit(Trigger.EditProfileAge, State.EditingProfileAge);

        _machine.Configure(State.EditingProfileAge)
            .Permit(Trigger.EditProfileDescription, State.EditingProfileDescription);

        _machine.Configure(State.EditingProfileDescription)
            .Permit(Trigger.EditProfilePicture, State.EditingProfilePicture);

        _machine.Configure(State.EditingProfilePicture)
            .Permit(Trigger.ViewProfileSummary, State.ViewingProfileSummary);

        _machine.Configure(State.ViewingProfileSummary)
            .Permit(Trigger.ViewProfiles, State.ViewingProfiles)
            .Permit(Trigger.EditProfileName, State.EditingProfileName);
    }

    public string Dot() => UmlDotGraph.Format(_machine.GetInfo());

    public async Task EditProfileNameAsync() => await _machine.FireAsync(Trigger.EditProfileName);
    public async Task EditProfileAgeAsync() => await _machine.FireAsync(Trigger.EditProfileAge);
    public async Task EditProfileDescriptionAsync() => await _machine.FireAsync(Trigger.EditProfileDescription);
    public async Task EditProfilePictureAsync() => await _machine.FireAsync(Trigger.EditProfilePicture);
    public async Task ViewProfileSummaryAsync() => await _machine.FireAsync(Trigger.ViewProfileSummary);
    public async Task ViewProfilesAsync() => await _machine.FireAsync(Trigger.ViewProfiles);
}