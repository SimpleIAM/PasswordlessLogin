<template>
    <div>
        {{message}}
    </div>
</template>

<script>
import Vue from 'vue';
const zxcvbn = () => import(/* webpackChunkName: "pwstrength" */ 'zxcvbn');

export default {
    data: function () {
        return {
            message: 'test'
        }
    }
}
</script>
