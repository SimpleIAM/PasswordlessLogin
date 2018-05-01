const oneTimeCodeEl = document.getElementById("OneTimeCode");
const newPassEl = document.getElementById("NewPassword");
const confirmPassEl = document.getElementById("ConfirmPassword");
const strengthEl = document.getElementById("passwordstrengthbits");
const feedbackEl = document.getElementById("passwordfeedback");
const setPasswordButtonEl = document.getElementById("setpassword");
const minStrength = newPassEl.dataset.minStrength || 0;
let strength_bits = 0;

analysePassword();
enableSubmitIfFormValid();

oneTimeCodeEl.addEventListener('keyup', function (e) {
    enableSubmitIfFormValid();
});

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
        feedback = feedback + '<li class="warning">' + pw_analysis.feedback.warning + '</li>';
    }
    pw_analysis.feedback.suggestions.forEach(function (suggestion) {
        feedback = feedback + '<li>' + suggestion + '</li>';
    });
    if (feedback) {
        feedback = '<ul class="hint">' + feedback + '</ul>';
    }
    feedbackEl.innerHTML = feedback;
}

function enableSubmitIfFormValid() {
    const oneTimeCodePresent = oneTimeCodeEl.value.length >= 6;
    const passwordsMatch = confirmPassEl.value === newPassEl.value;
    if (!passwordsMatch) {
        //todo: warn
    }
    const passwordStrongEnough = strength_bits > minStrength;

    setPasswordButtonEl.disabled = !(oneTimeCodePresent && passwordsMatch && passwordStrongEnough);
}
