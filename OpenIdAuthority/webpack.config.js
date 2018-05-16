const path = require('path')
const VueLoaderPlugin = require('vue-loader/lib/plugin')

module.exports = {
  entry: {
    main: './UI/Main.js'
  },
  output: {
    filename: 'dist.[name].js',
    chunkFilename: 'dist.[name].[chunkhash:12].js',
    path: path.resolve(__dirname, 'wwwroot')
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