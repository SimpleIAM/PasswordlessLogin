<template>
  <div class="register">
    <form @submit.prevent="submitForm" class="form">
      <div class="field field-stacked form_row">
        <label class="field_label" for="email">Email</label>
        <input class="field_element field_element-fullWidth register_email"
          ref="email"
          id="email" 
          v-model="email" 
          type="text" 
          placeholder="you@example.com"
          >
        <span v-if="emailError" class="field_error">{{emailError}}</span>
      </div>

      <div class="field field-checkbox form_row">
        <input 
          class="field_element"
          type="checkbox" 
          id="consent" 
          v-model="consent">
        <label class="field_label register_consentLabel" for="consent">I consent to the <a href="/privacy" target="_blank">privacy policy</a> and <a href="/terms" target="_blank">terms of service</a>.</label>
      </div>

      <div class="field form_row">
        <button 
          :disabled="!emailIsValid || !consent"
          type="submit"
          class="field_element field_element-fullWidth field_element-tall register_button"
          >Register
        </button>
      </div>
      <div class="message message-notice register_message" v-if="message">
        {{message}}
      </div>
    </form>
  </div>
</template>

<script>
import Vue from 'vue';
import api from '../../api.js';

var VueCookie = require('vue-cookie');

Vue.use(VueCookie);

export default {
  props: ['nextUrl'],
  data: function() {
    return {
      email: '',
      emailError: '',
      consent: false,
      message: ''
    };
  },
  computed: {
    emailIsValid: function() {
      if(this.email.length == 0) {
        return false;
      }
      var testEl = document.createElement('input');
      testEl.type = 'email';
      testEl.value = this.email;
      return testEl.checkValidity();
    }
  },
  methods: {
    submitForm: function() {
      this.message = "Please wait...";
      api.register(null, this.email, this.nextUrl)
        .then(data => {
          this.message = data.message ? data.message : 'Success';
        })
        .catch(error => {          
          this.message = error.message ? error.message : 'Something went wrong';         
        });
    }
  },
  mounted: function() {
    this.$nextTick(() => {
      this.$refs.email.focus();
    });
  }
};
</script>
