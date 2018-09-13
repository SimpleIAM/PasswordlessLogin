<template>
  <div class="account">
    <div class="form_row field field-stacked">
      <label class="field_label">Sign in name</label>
      <div>{{email}}</div>
    </div>
    <div class="form_row field field-stacked">
      <label class="field_label">Password</label>
      <div v-if="datePasswordSet">Set on {{localDatePasswordSet}}</div>
      <div v-else>None</div>
      <a href="/account/setpassword"><span v-if="datePasswordSet">change</span><span v-else>set</span> password</a>
      <span v-if="datePasswordSet"> &bull; <a @click.prevent="removePassword" href="">remove password</a></span>
    </div>
    <div class="form_row field field-stacked" v-for="(value, key) in additionalProperties" v-if="value">
      <label class="field_label">{{key}}</label>
      <div>{{value}}</div>
    </div>
    <div class="message message-notice register_message" v-if="message">
      {{message}}
    </div>
  </div>
</template>

<script>
import Vue from 'vue';
import api from '../../securedapi.js';
//const zxcvbn = () => import(/* webpackChunkName: "pwstrength" */ 'zxcvbn');

export default {
  props: ['nextUrl'],
  data: function() {
    return {
      initialized: false,
      email: '',
      additionalProperties: {},
      datePasswordSet: null,
      message: null,
    };
  },
  computed: {
    localDatePasswordSet() {
      const date = new Date(this.datePasswordSet + 'Z');
      return date.toLocaleDateString();
    }
  },
  methods: {
    removePassword() {
      api.removePassword().then(() => {
        this.datePasswordSet = null;
      })
      .catch(err => {
        this.message = err.message;
      });      
    },
  },
  beforeMount() {
    api.getMyAccount().then((data) => {
      this.email = data.email;
      this.additionalProperties = data;
      this.additionalProperties.sub = null;
      this.additionalProperties.email = null;
      this.initialized = true;
    });
    api.getDatePasswordSet().then((data) => {
      this.datePasswordSet = data.date;
    });
  },
};
</script>