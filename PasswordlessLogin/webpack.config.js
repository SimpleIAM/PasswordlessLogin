const path = require('path')
const VueLoaderPlugin = require('vue-loader/lib/plugin')

module.exports = {
  entry: {
    main: './UI/Main.js'
  },
  output: {
    filename: '[name].js',
    chunkFilename: '[name].[chunkhash:12].js',
    path: path.resolve(__dirname, 'wwwroot/passwordless/dist'),
    publicPath: '/passwordless.dist.'
  },
  module: {
    rules: [
      { test: /\.vue$/, include: /UI/, loader: 'vue-loader' },
      { 
        test: /\.js$/, 
        loader: 'babel-loader',
        exclude: /node_modules/, 
        options: {
          babelrc: false,
          presets: [['es2015', { modules: false }], 'stage-3']
        }
      }
    ]
  },
  plugins: [
    new VueLoaderPlugin()
  ],
  /*optimization: {
      splitChunks: {
          cacheGroups: {
            common: {
              name: 'common',
              chunks: 'all',
              minChunks: 2,
              enforce: true
            }
          },
      },
      runtimeChunk: false
  },*/
  devtool: '#source-map'
};