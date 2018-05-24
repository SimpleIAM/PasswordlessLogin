const newPassEl = document.getElementById("NewPassword");
const confirmPassEl = document.getElementById("ConfirmPassword");
const strengthEl = document.getElementById("passwordstrengthbits");
const feedbackEl = document.getElementById("passwordfeedback");
const confirmErrorEl = document.getElementById("confirm-password-error");
const setPasswordButtonEl = document.getElementById("setpassword");
const skipButtonEl = document.getElementById("skip");
const minStrength = newPassEl.dataset.minStrength || 0;
let strength_bits = 0;

analysePassword();
enableSubmitIfFormValid();

newPassEl.addEventListener('keyup', function (e) {
    let capsLock = e.getModifierState && e.getModifierState('CapsLock'); //todo: warn
    analysePassword();
    enableSubmitIfFormValid();
});

confirmPassEl.addEventListener('keyup', function (e) {
    let capsLock = e.getModifierState && e.getModifierState('CapsLock'); //todo: warn
    enableSubmitIfFormValid();    
});

function analysePassword() {
    let pw_analysis = zxcvbn(newPassEl.value);
    strength_bits = Math.floor(Math.log2(pw_analysis.guesses));
    strengthEl.innerText = strength_bits;
    let feedback = '';
    if (pw_analysis.feedback.warning) {
        feedback = feedback + '<li class="passwordFeedback passwordFeedback-warning">' + pw_analysis.feedback.warning + '</li>';
    }
    pw_analysis.feedback.suggestions.forEach(function (suggestion) {
        feedback = feedback + '<li class="passwordFeedback">' + suggestion + '</li>';
    });
    if (feedback) {
        feedback = '<ul>' + feedback + '</ul>';
    }
    feedbackEl.innerHTML = feedback;
}

function enableSubmitIfFormValid() {
    const passwordStrongEnough = strength_bits >= minStrength;

    const passwordsMatch = confirmPassEl.value === newPassEl.value;
    confirmErrorEl.innerText = passwordsMatch ? "" : "Passwords don't match";

    setPasswordButtonEl.disabled = !(passwordStrongEnough && passwordsMatch);
    if(skipButtonEl) {
        skipButtonEl.disabled = (confirmPassEl.value.length > 0);
    }
}
